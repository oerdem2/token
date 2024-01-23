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
using System.Text;

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
    private readonly IbDatabaseContext _ibContext;
    public LoginController(ILogger<TokenController> logger, IAuthorizationService authorizationService, IUserService userService, DatabaseContext databaseContext
    , IConfiguration configuration, DaprClient daprClient, IClientService clientService, IInternetBankingUserService ibUserService, ITransactionService transactionService,
    IFlowHandler flowHandler, IConsentService consentService, IProfileService profileService,IbDatabaseContext ibContext)
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
        _ibContext = ibContext;
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("public/Authorize")]
    public async Task<IActionResult> OpenBankingAuthorize(Guid riza_no)
    {
        var consent = await _consentService.GetConsent(riza_no);

        return View();
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
            var userResponse = await _ibUserService.GetUser(openBankingLoginRequest.username!);
            if (userResponse.StatusCode != 200)
            {
                //TODO
                return StatusCode(500);
            }
            
            var passwordResponse = await _ibUserService.GetPassword(userResponse.Response.Id);
            if (passwordResponse.StatusCode != 200)
            {
                //TODO
                return StatusCode(500);
            }
            var passwordRecord = passwordResponse.Response;

            var isVerified = _ibUserService.VerifyPassword(passwordRecord!.HashedPassword!, openBankingLoginRequest.password!, passwordRecord.Id.ToString());
            //Consider SuccessRehashNeeded
            if (isVerified != PasswordVerificationResult.Success)
            {
                passwordRecord.AccessFailedCount = (passwordRecord.AccessFailedCount ?? 0) + 1;
                //TODO - Disable User After Several Failed Login Attemps
                await _ibContext.SaveChangesAsync();
                return StatusCode(500);
            }
            else
            {
                passwordRecord.AccessFailedCount = 0;
                await _ibContext.SaveChangesAsync();
            }



            var userInfoResult = await _profileService.GetCustomerSimpleProfile(openBankingLoginRequest.username!);
            if (userInfoResult.StatusCode != 200)
            {
                //TODO
                return StatusCode(500);
            }

            var userInfo = userInfoResult.Response;

            if (userInfo!.data!.profile!.Equals("customer") || !userInfo!.data!.profile!.status!.Equals("active"))
            {
                //TODO
                return StatusCode(500);
            }

            var mobilePhoneCount = userInfo!.data!.phones!.Count(p => p.type!.Equals("mobile"));
            if (mobilePhoneCount != 1)
            {
                //TODO
                return StatusCode(500);
            }

            var mobilePhone = userInfo!.data!.phones!.FirstOrDefault(p => p.type!.Equals("mobile"));
            if (string.IsNullOrWhiteSpace(mobilePhone!.prefix) || string.IsNullOrWhiteSpace(mobilePhone!.number))
            {
                //TODO
                return StatusCode(500);
            }

            var userRequest = new UserInfo
            {
                firstName = userInfo!.data.profile!.name!,
                lastName = userInfo!.data.profile!.name!,
                phone = new core.Models.User.UserPhone()
                {
                    countryCode = mobilePhone!.countryCode!,
                    prefix = mobilePhone!.prefix,
                    number = mobilePhone!.number
                },
                state = "Active",
                salt = passwordRecord.Id.ToString(),
                password = openBankingLoginRequest.password!,
                explanation = "Migrated From IB",
                reason = "Amorphie Login",
                isArgonHash = true
            };

            var verifiedMailAddress = userInfo.data.emails!.FirstOrDefault(m => m.isVerified == true);
            userRequest.eMail = verifiedMailAddress?.address ?? "";
            userRequest.reference = openBankingLoginRequest.username!;

            var migrateResult = await _userService.SaveUser(userRequest);

            var amorphieUserResult = await _userService.Login(new LoginRequest() { Reference = openBankingLoginRequest.username!, Password = openBankingLoginRequest.password! });
            var amorphieUser = amorphieUserResult.Response;

            var rand = new Random();
            var code = String.Empty;

            for (int i = 0; i < 6; i++)
            {
                code += rand.Next(10);
            }

            var transactionId = Guid.NewGuid();
            await _daprClient.SaveStateAsync(_configuration["DAPR_STATE_STORE_NAME"], $"{transactionId}_Login_Otp_Code", code);


            var otpRequest = new
            {
                Sender = "AutoDetect",
                SmsType = "Otp",
                Phone = new
                {
                    CountryCode = amorphieUser.MobilePhone!.CountryCode,
                    Prefix = amorphieUser.MobilePhone.Prefix,
                    Number = amorphieUser.MobilePhone.Number
                },
                Content = $"{code} şifresi ile giriş yapabilirsiniz",
                Process = new
                {
                    Name = "Token Login Flow",
                    Identity = "Otp Login"
                }
            };

            StringContent request = new(JsonSerializer.Serialize(otpRequest), Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            var httpResponse = await httpClient.PostAsync(_configuration["MessagingGatewayUri"], request);

            if (!httpResponse.IsSuccessStatusCode)
            {
                //TODO
            }

            

            return View("Otp",new Otp
            {
                Phone = "0" + amorphieUser.MobilePhone.Prefix.ToString().Substring(0, 2) + "******" + amorphieUser.MobilePhone.Number.ToString().Substring(amorphieUser.MobilePhone.Number.Length - 2, 2),
                transactionId = transactionId,
                consentId = openBankingLoginRequest.consentId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            return StatusCode(500);
        }
    }

    [HttpPost("public/ValidateOtp")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ValidateOtp(Otp otpRequest)
    {
        var consentResult = await _consentService.GetConsent(otpRequest.consentId);
        if (consentResult.StatusCode != 200)
        {
            ViewBag.ErrorDetail = consentResult.Detail;
            return View("Error");
        }
        var consent = consentResult.Response;

        var sendedOtpValue = await _daprClient.GetStateAsync<string>(_configuration["DAPR_STATE_STORE_NAME"], $"{otpRequest.transactionId}_Login_Otp_Code");
        if (sendedOtpValue.Equals(otpRequest.OtpValue))
        {
            if(consent.consentType.Equals("OB_Account"))
            {
                return Redirect(_configuration["OpenBankingAccount"]+otpRequest.consentId);
            }
            if(consent.consentType.Equals("OB_Payment"))
            {
                return Redirect(_configuration["OpenBankingPayment"]+otpRequest.consentId);
            }
            return Forbid();
        }
        else
        {
            return Forbid();
        }
    }

    private async Task<IActionResult> WorkflowProcess()
    {
        var transaction = _transactionService.Transaction;

        while (transaction!.TransactionNextEvent == TransactionNextEvent.Waiting)
        {
            await _transactionService.ReloadTransaction();
            transaction = _transactionService.Transaction;

            await Task.Delay(100);
        }

        if (transaction.TransactionNextEvent == TransactionNextEvent.ShowPage)
        {
            if (transaction.TransactionNextPage == TransactionNextPage.Login)
            {
                var loginModel = new Login()
                {

                };
                ViewBag.HasError = false;
                return View("LoginPage", loginModel);
            }
            if (transaction.TransactionNextPage == TransactionNextPage.Otp)
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

        if (transaction.TransactionNextEvent == TransactionNextEvent.PublishMessage)
        {
            dynamic zeebeMessage = new ExpandoObject();
            zeebeMessage.messageName = transaction.TransactionNextMessage;
            zeebeMessage.correlationKey = transaction.Id;
            await _daprClient.InvokeBindingAsync("zeebe-local", "publish-message", zeebeMessage);

            transaction.TransactionNextEvent = TransactionNextEvent.Waiting;
            await _transactionService.SaveTransaction(transaction);
        }


        return await WorkflowProcess();
    }
}
