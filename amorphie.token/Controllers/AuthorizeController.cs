using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using amorphie.token.data;
using Login = amorphie.token.core.Models.Account.Login;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Consent;
using amorphie.token.Services.TransactionHandler;
using amorphie.token.core.Extensions;
using System.Dynamic;
using Azure;
using amorphie.token.core.Models.Transaction;

namespace amorphie.token.core.Controllers;

public class AuthorizeController : Controller
{
    private readonly ILogger<AuthorizeController> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserService _userService;
    private readonly IClientService _clientService;
    private readonly IInternetBankingUserService _ibUserService;
    private readonly DatabaseContext _databaseContext;
    private readonly IConfiguration _configuration;
    private readonly IFlowHandler _flowHandler;
    private readonly DaprClient _daprClient;
    private readonly ITransactionService _transactionService;
    private readonly IConsentService _consentService;
    private readonly IProfileService _profileService;
    public AuthorizeController(ILogger<AuthorizeController> logger, IAuthorizationService authorizationService, IUserService userService, DatabaseContext databaseContext
    , IConfiguration configuration, DaprClient daprClient, IClientService clientService, IInternetBankingUserService ibUserService,ITransactionService transactionService,
    IFlowHandler flowHandler,IConsentService consentService,IProfileService profileService)
    {
        _logger = logger;
        _authorizationService = authorizationService;
        _userService = userService;
        _databaseContext = databaseContext;
        _configuration = configuration;
        _flowHandler = flowHandler;
        _clientService = clientService;
        _ibUserService = ibUserService;
        _transactionService = transactionService;
        _daprClient = daprClient;
        _consentService = consentService;
        _profileService = profileService;
    }

    [HttpGet("public/OpenBankingAuthorize")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OpenBankingAuthorize(OpenBankingAuthorizationRequest authorizationRequest)
    {
        Models.Transaction.Transaction transaction = new(){
            Id = Guid.NewGuid(),
            TransactionState = TransactionState.Active
        };
        var consentResult = await _consentService.GetConsent(authorizationRequest.riza_no);
        if(consentResult.StatusCode != 200)
        {
            ViewBag.ErrorDetail = consentResult.Detail;
            return View("Error");
        }
        var consent = consentResult.Response;

        var consentData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(consent!.additionalData!);

        string kmlkNo = consentData!.kmlk.kmlkVrs.ToString();

        var customerInfoResult = await _profileService.GetCustomerProfile(kmlkNo);
        if(customerInfoResult.StatusCode != 200)
        {
            ViewBag.ErrorDetail = customerInfoResult.Detail;
            return View("Error");
        }
        var customerInfo = customerInfoResult.Response;
        transaction.Profile = customerInfo!;
        transaction.ConsentData = consent;
        ViewBag.TransactionId = transaction.Id;

        await _transactionService.SaveTransaction(transaction);

        var loginModel = new OpenBankingLogin
        {
            transactionId = _transactionService.Transaction!.Id.ToString()
        };

        if(customerInfo!.businessLine == "X")
        {
            return View("OpenBankingLoginOn",loginModel);
        }
        else
        {
            return View("OpenBankingLoginBurgan",loginModel);
        }
        
    }

    [HttpGet("public/Authorize")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Authorize(AuthorizationRequest authorizationRequest)
    {
        var transaction = _transactionService.Transaction;
        transaction!.TransactionNextPage = TransactionNextPage.Otp;
        transaction!.AuthorizationReqest = authorizationRequest;
        await _transactionService.SaveTransaction(transaction);

        dynamic zeebeCommand = new ExpandoObject();
        zeebeCommand.messageName = "authorize-flow-start";
        dynamic variables = new ExpandoObject();
        variables.TransactionId = transaction!.Id;
        variables.AuthorizeRequest = authorizationRequest;
        var loggedUser = HttpContext.Session.GetString("LoggedUser");
        if(loggedUser != null)
            variables.User = JsonSerializer.Deserialize<LoginResponse>(HttpContext.Session.GetString("LoggedUser")!);
        
        zeebeCommand.variables = variables;
        await _daprClient.InvokeBindingAsync("zeebe-local","publish-message",zeebeCommand);

        return await WorkflowProcess();

        zeebeCommand.variables = variables;
        
        
        var authorizationServiceRequest = authorizationRequest.MapTo<AuthorizationServiceRequest>();
        var authorizationResponse = await _authorizationService.Authorize(authorizationServiceRequest);

        if (authorizationResponse.StatusCode != 200)
        {
            return Content($"An Error Occured. Detail : " + authorizationResponse.Detail);
        }

        var authorizationResult = authorizationResponse.Response;

        if (HttpContext.Session.Get("LoggedUser") == null)
        {
            var loginModel = new Login()
            {
                Code = authorizationResult!.Code,
                RedirectUri = authorizationResult.RedirectUri,
                RequestedScopes = authorizationResult.RequestedScopes
            };
            ViewBag.HasError = false;
            return View("Login", loginModel);
        }



        return Redirect($"{authorizationResult.RedirectUri}&code={authorizationResult.Code}");

    }


    private async Task<IActionResult> WorkflowProcess()
    {
        var transaction = _transactionService.Transaction;

        while(transaction!.TransactionNextEvent == TransactionNextEvent.Waiting)
        {
            await _transactionService.ReloadTransaction();
            transaction = _transactionService.Transaction;
            
            await Task.Delay(10);
        }

        Console.WriteLine("Transaction Not Waited Anymore");
        Console.WriteLine("Transaction Next State: "+transaction.TransactionNextEvent);
        if(transaction.TransactionNextEvent == TransactionNextEvent.ShowPage)
        {
            if(transaction.TransactionNextPage == TransactionNextPage.Login)
            {
                var loginModel = new Login()
                {
                    TransactionId = transaction.Id
                };
                ViewBag.HasError = false;

                transaction.TransactionNextEvent = TransactionNextEvent.Waiting;
                await _transactionService.SaveTransaction(transaction);

                return View("LoginPage", loginModel);
            }
        }

        if(transaction.TransactionNextEvent == TransactionNextEvent.PublishMessage)
        {
            dynamic zeebeMessage = new ExpandoObject();
            zeebeMessage.messageName = "amorphie-oauth-session-off";
            zeebeMessage.correlationKey = transaction.Id;
            await _daprClient.InvokeBindingAsync("zeebe-local","publish-message",zeebeMessage);
        }

        transaction.TransactionNextEvent = TransactionNextEvent.Waiting;
        await _transactionService.SaveTransaction(transaction);
        
        return await WorkflowProcess();
    }

}
