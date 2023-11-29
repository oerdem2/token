using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using amorphie.token.data;
using Login = amorphie.token.core.Models.Account.Login;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Consent;
using System.Dynamic;
using amorphie.token.Services.TransactionHandler;
using amorphie.core.Enums;
using System.Reflection.Metadata.Ecma335;

namespace amorphie.token.core.Controllers;

public class LoginController : Controller
{
    private readonly ILogger<TokenController> _logger;
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
    public LoginController(ILogger<TokenController> logger, IAuthorizationService authorizationService, IUserService userService, DatabaseContext databaseContext
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

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("public/LoginFlow")]
    public async Task<IActionResult> LoginFlow(Login loginRequest)
    {
        try
        {
            await _transactionService.GetTransaction(loginRequest.TransactionId);
            var transaction = _transactionService.Transaction;
            dynamic message = new ExpandoObject();
            message.messageName = "amorphie-oauth-first-factor";
            message.correlationKey = transaction!.Id.ToString();
            dynamic variables = new ExpandoObject();
            variables.username = loginRequest.UserName;
            variables.password = loginRequest.Password;
            message.variables = variables;
            await _daprClient.InvokeBindingAsync("zeebe-local","publish-message",message);

            return await WorkflowProcess();
        }
        catch(Exception ex)
        {
            return BadRequest();
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("public/OtpFlow")]
    public async Task<IActionResult> OtpFlow(Otp otpRequest)
    {
        try
        {
            await _transactionService.GetTransaction(otpRequest.transactionId);
            var transaction = _transactionService.Transaction;
            dynamic message = new ExpandoObject();
            message.messageName = "amorphie-oauth-first-factor";
            message.correlationKey = transaction!.Id.ToString();
            dynamic variables = new ExpandoObject();
            variables.otpValue = otpRequest.OtpValue;
            
            message.variables = variables;
            await _daprClient.InvokeBindingAsync("zeebe-local","publish-message",message);

            return await WorkflowProcess();
        }
        catch(Exception ex)
        {
            return BadRequest();
        }
    }
    
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("public/Login")]
    public async Task<IActionResult> Login(Login loginRequest)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(loginRequest.UserName) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                ViewBag.HasError = true;
                ViewBag.ErrorDetail = "Reference and Password Can Not Be Empty";
            }
            var userResponse = await _userService.Login(new LoginRequest() { Reference = loginRequest.UserName!, Password = loginRequest.Password! });
            if (userResponse.StatusCode != 200)
            {
                ViewBag.HasError = true;
                ViewBag.ErrorDetail = userResponse.Detail;
                var loginModel = new Login()
                {
                    Code = loginRequest.Code,
                    RedirectUri = loginRequest.RedirectUri,
                    RequestedScopes = loginRequest.RequestedScopes
                };
                return View("Login", loginModel);
            }
            var user = userResponse.Response;

            if (user?.State.ToLower() == "active" || user?.State.ToLower() == "new")
            {
                HttpContext.Session.SetString("LoggedUser", JsonSerializer.Serialize(user));
                await _authorizationService.AssignUserToAuthorizationCode(user, loginRequest.Code!);

                return Redirect($"{loginRequest.RedirectUri}&code={loginRequest.Code}");
            }
            else
            {
                ViewBag.HasError = true;
                ViewBag.ErrorDetail = "User Is Disabled";
                var loginModel = new Login()
                {
                    Code = loginRequest.Code,
                    RedirectUri = loginRequest.RedirectUri,
                    RequestedScopes = loginRequest.RequestedScopes
                };
                return View("Login", loginModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            return StatusCode(500);
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("public/OpenBankingLogin")]
    public async Task<IActionResult> OpenBankingLogin(OpenBankingLogin openBankingLoginRequest)
    {
        try
        {
            dynamic message = new ExpandoObject();

            message.messageName = "openbanking-login-process";
            message.variables = openBankingLoginRequest;

            _daprClient.InvokeBindingAsync("zeebe-local","publish-message",message);
            await _transactionService.GetTransaction(Guid.Parse(openBankingLoginRequest.transactionId));
            while(_transactionService.Transaction!.TransactionState == TransactionState.Active && _transactionService.Transaction.Next == false)
            {
                await _transactionService.GetTransaction(Guid.Parse(openBankingLoginRequest.transactionId));
                await Task.Delay(100);
            }
            if(_transactionService.Transaction.TransactionState == TransactionState.Error)
            {
                ViewBag.ErrorDetail = "Bir hata oluştu. Daha sonra tekrar deneyiniz";
                return View("error");
            }
            if(_transactionService.Transaction.Next)
            {
                var transaction = _transactionService.Transaction;
                transaction.Next = false;
                await _transactionService.SaveTransaction(transaction);

                var otpModel = new Otp
                {
                    transactionId = _transactionService.Transaction.Id,
                    Phone = _transactionService.Transaction.User!.MobilePhone!.ToString()
                };


                if (_transactionService.Transaction.SecondFactorMethod == SecondFactorMethod.Otp)
                {
                    return View("Otp",otpModel);
                }
                else
                {
                    return View("Otp",otpModel);
                }
            }

            ViewBag.ErrorDetail = "Bir hata oluştu. Daha sonra tekrar deneyiniz";
            return View("error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            return StatusCode(500);
        }
    }

    private async Task<IActionResult> WorkflowProcess()
    {
        var transaction = _transactionService.Transaction;
        
        while(transaction!.TransactionNextEvent == TransactionNextEvent.Waiting)
        {
            await _transactionService.ReloadTransaction();
            transaction = _transactionService.Transaction;
            
            await Task.Delay(100);
        }

        Console.WriteLine("Transaction Not Waited Anymore");
        Console.WriteLine("Transaction Next State: "+transaction.TransactionNextEvent);
        if(transaction.TransactionNextEvent == TransactionNextEvent.ShowPage)
        {
            if(transaction.TransactionNextPage == TransactionNextPage.Login)
            {
                var loginModel = new Login()
                {
                    
                };
                ViewBag.HasError = false;
                return View("LoginPage", loginModel);
            }
            if(transaction.TransactionNextPage == TransactionNextPage.Otp)
            {
                var otpModel = new Otp()
                {
                    transactionId = transaction.Id
                };
                ViewBag.HasError = false;

                transaction.TransactionNextEvent = TransactionNextEvent.Waiting;
                await _transactionService.SaveTransaction(transaction);

                return View("Otp", otpModel);
            }
        }

        if(transaction.TransactionNextEvent == TransactionNextEvent.PublishMessage)
        {
            dynamic zeebeMessage = new ExpandoObject();
            zeebeMessage.messageName = transaction.TransactionNextMessage;
            zeebeMessage.correlationKey = transaction.Id;
            await _daprClient.InvokeBindingAsync("zeebe-local","publish-message",zeebeMessage);

            transaction.TransactionNextEvent = TransactionNextEvent.Waiting;
            await _transactionService.SaveTransaction(transaction);
        }

        
        return await WorkflowProcess();
    }
}
