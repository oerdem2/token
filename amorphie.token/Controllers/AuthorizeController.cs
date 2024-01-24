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
using amorphie.token.core.Models.Workflow;

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
    , IConfiguration configuration, DaprClient daprClient, IClientService clientService, IInternetBankingUserService ibUserService, ITransactionService transactionService,
    IFlowHandler flowHandler, IConsentService consentService, IProfileService profileService)
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

    [HttpGet("/public/OpenBankingAuthCode")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OpenBankingAuthCode(Guid consentId)
    {
        var consentResponse = await _consentService.GetConsent(consentId);
        if (consentResponse.StatusCode == 200)
        {
            var consent = consentResponse.Response;
            var deserializedData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(consent.additionalData);
            var redirectUri = deserializedData.gkd.yonAdr;
            var authResponse = await _authorizationService.Authorize(new AuthorizationServiceRequest()
            {
                ResponseType = "code",
                ClientId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                Scope = new string[] { "openbanking-customer" },
                ConsentId = consentId
            });
            var authCode = authResponse.Response.Code;
            return Redirect($"{redirectUri}&rizaDrm=Y&yetKod={authCode}&rizaNo={consentId}&rizaTip=H");
        }
        return Forbid();
    }

    [HttpGet("public/OpenBankingAuthorize")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OpenBankingAuthorize(OpenBankingAuthorizationRequest authorizationRequest)
    {

        var consentResult = await _consentService.GetConsent(authorizationRequest.riza_no);
        if (consentResult.StatusCode != 200)
        {
            ViewBag.ErrorDetail = consentResult.Detail;
            return View("Error");
        }
        var consent = consentResult.Response;

        var consentData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(consent!.additionalData!);
        string kmlkNo = consentData!.kmlk.kmlkVrs.ToString();
        var customerInfoResult = await _profileService.GetCustomerSimpleProfile(kmlkNo);
        if (customerInfoResult.StatusCode != 200)
        {
            ViewBag.ErrorDetail = customerInfoResult.Detail;
            return View("Error");
        }
        var customerInfo = customerInfoResult.Response;

        var loginModel = new OpenBankingLogin
        {
            consentId = authorizationRequest.riza_no
        };

        if (customerInfo!.data!.profile!.businessLine == "X")
        {
            return View("OpenBankingLoginOn", loginModel);
        }
        else
        {
            return View("OpenBankingLoginBurgan", loginModel);
        }

    }

    [HttpGet("public/Authorize")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Authorize(AuthorizationRequest authorizationRequest)
    {
        using var httpClient = new HttpClient();
        var workflowRequest = new WorkflowPostTransitionRequest
        {
            EntityData = JsonSerializer.Serialize(authorizationRequest),
            GetSignalRHub = true
        };

        StringContent request = new(JsonSerializer.Serialize(workflowRequest), Encoding.UTF8, "application/json");
        request.Headers.Add("User", Guid.NewGuid().ToString());
        request.Headers.Add("Behalf-Of-User", Guid.NewGuid().ToString());

        var httpResponse = await httpClient.PostAsync(_configuration["workflowAuthorizeUri"]!.Replace("{{recordId}}", Guid.NewGuid().ToString()), request);
        return View("Waiting");
    }


    private async Task<IActionResult> WorkflowProcess()
    {
        var transaction = _transactionService.Transaction;

        while (transaction!.TransactionNextEvent == TransactionNextEvent.Waiting)
        {
            await _transactionService.ReloadTransaction();
            transaction = _transactionService.Transaction;

            await Task.Delay(10);
        }

        if (transaction.TransactionNextEvent == TransactionNextEvent.ShowPage)
        {
            if (transaction.TransactionNextPage == TransactionNextPage.Login)
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

        if (transaction.TransactionNextEvent == TransactionNextEvent.PublishMessage)
        {
            dynamic zeebeMessage = new ExpandoObject();
            zeebeMessage.messageName = "amorphie-oauth-session-off";
            zeebeMessage.correlationKey = transaction.Id;
            await _daprClient.InvokeBindingAsync("zeebe-local", "publish-message", zeebeMessage);
        }

        transaction.TransactionNextEvent = TransactionNextEvent.Waiting;
        await _transactionService.SaveTransaction(transaction);

        return await WorkflowProcess();
    }

}
