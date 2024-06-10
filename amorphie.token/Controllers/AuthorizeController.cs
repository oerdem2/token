using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using amorphie.token.data;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Consent;
using amorphie.token.Services.TransactionHandler;
using amorphie.token.Services.Login;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;


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
    private readonly ILoginService _loginService;
    public AuthorizeController(ILogger<AuthorizeController> logger, IAuthorizationService authorizationService, IUserService userService, DatabaseContext databaseContext
    , IConfiguration configuration, DaprClient daprClient, IClientService clientService, IInternetBankingUserService ibUserService, ITransactionService transactionService,
    IFlowHandler flowHandler, IConsentService consentService, IProfileService profileService, ILoginService loginService)
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
        _loginService = loginService;
    }


    [HttpPost("/public/get-user-info")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetUserInfo([FromForm] string access_token)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",access_token);
        var response = await httpClient.GetAsync(_configuration["GetUserInfoAddress"]);
        if(response.IsSuccessStatusCode)
        {
            var t = await response.Content.ReadAsStringAsync();
            var r = Newtonsoft.Json.JsonConvert.DeserializeObject<CustomerEntity>(await response.Content.ReadAsStringAsync());

            return new OkObjectResult(r);
        }
        else
        {
            return StatusCode((int)response.StatusCode);
        }
    }

    [HttpGet("/private/get-authorized-user-info")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetAuthorizedUserInfo()
    {
        await Task.CompletedTask;
        var obj = new{
                    TCKN = HttpContext.Request.Headers.FirstOrDefault(h => h.Key == "User_reference").Value.ToString(),
                    BusinessLine = HttpContext.Request.Headers.FirstOrDefault(h => h.Key == "Business_line").Value.ToString(),
                    CustomerNumber = HttpContext.Request.Headers.FirstOrDefault(h => h.Key == "Customer_no").Value.ToString(),
                    CustomerName = $"{HttpContext.Request.Headers.FirstOrDefault(h => h.Key == "Given_name").Value} {HttpContext.Request.Headers.FirstOrDefault(h => h.Key == "Family_name").Value}"
        };
        return Ok(obj);
    }

    [HttpGet("/public/GenerateAuthCode")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GenerateAuthCodeForTestingPurpose([FromQuery(Name = "Reference")] string reference)
    {
        if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!.Equals("Test") || 
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!.Equals("Development") ||
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!.Equals("Preprod"))
        {
            var migrateUser = await _loginService.MigrateDodgeUserToAmorphie(reference);
            var user = await _userService.GetUserByReference(reference);

            var codeVerifier = "sessionRedirect";
            var codeVerifierAsByte = System.Text.Encoding.ASCII.GetBytes(codeVerifier);
            using var sha256 = SHA256.Create();
            var hashedCodeVerifier = Base64UrlEncoder.Encode(sha256.ComputeHash(codeVerifierAsByte));
            var authCodeResponse = await _authorizationService.Authorize(new AuthorizationServiceRequest{
                ClientId = "IbAndroidApp",
                CodeChallange = hashedCodeVerifier,
                CodeChallangeMethod = "SHA256",
                Nonce = "test",
                State = "test",
                User = user.Response,
                ResponseType = "code",
                Scope = ["retail-customer","openId"]
            });

            return Ok(new{
                AuthCode = authCodeResponse!.Response!.Code,
                CodeVerifier = codeVerifier
            });
        }
        return StatusCode(401);
    }

    [HttpGet("/public/open-banking-generate-auth-code")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OpenBankingGenerateAuthCode([FromQuery(Name = "rizaNo")] Guid consentId, [FromQuery(Name = "rizaTip")] string consentType)
    {
        var consentResponse = await _consentService.GetConsent(consentId);
        
        if (consentResponse.StatusCode == 200)
        {
            var consent = consentResponse!.Response!;
            var consentData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(consent!.additionalData!);
            string kmlkNo = string.Empty;
            if (consent.consentType!.Equals("OB_Account"))
            {
                kmlkNo = consentData!.kmlk.kmlkVrs.ToString();
            }
            if (consent.consentType!.Equals("OB_Payment"))
            {
                kmlkNo = consentData!.odmBsltm.kmlk.kmlkVrs.ToString();
            }
            var userResponse = await _userService.GetUserByReference(kmlkNo);
            if(userResponse.StatusCode != 200)
            {
                //TODO 
                //Error Handle
            }
            var user = userResponse.Response;
            var redirectUri = consentData!.gkd.yonAdr;

            var authResponse = await _authorizationService.Authorize(new AuthorizationServiceRequest()
            {
                ResponseType = "code",
                ClientId = _configuration["OpenBankingClientId"],
                Scope = ["open-banking"],
                ConsentId = consentId,
                User = user
            });
            var authCode = authResponse.Response!.Code;
            return StatusCode(201,new{
                yetkilendirmeKodu = new{
                    yetKod = authCode,
                    rizaNo = consentId,
                    rizaDrm =  "Y"
                }
            });
        }
        return Forbid();
    }

    [HttpGet("/public/OpenBankingAuthCode")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> OpenBankingAuthCode(Guid consentId)
    {
        var consentResponse = await _consentService.GetConsent(consentId);
        var user = await _daprClient.GetStateAsync<LoginResponse>(_configuration["DAPR_STATE_STORE_NAME"], $"{consentId}_User");

        if (consentResponse.StatusCode == 200)
        {
            var consent = consentResponse!.Response!;
            var deserializedData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(consent.additionalData!);
            
            var redirectUri = deserializedData.gkd.yonAdr;

            var authResponse = await _authorizationService.Authorize(new AuthorizationServiceRequest()
            {
                ResponseType = "code",
                ClientId = _configuration["OpenBankingClientId"],
                Scope = ["open-banking"],
                ConsentId = consentId,
                User = user
            });
            var authCode = authResponse.Response.Code;

            return Redirect($"{redirectUri}&rizaDrm=Y&yetKod={authCode}&rizaNo={consentId}&rizaTip={OpenBankingConstants.ConsentTypeMap[consent.consentType!]}");
        }
        return Forbid();
    }

    [HttpPost("public/CreatePreLogin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> CreatePreLogin([FromHeader(Name = "clientIdReal")] string sourceClient, [FromHeader(Name = "user_reference")] string currentUser, [FromHeader(Name = "scope")] string[] scope, [FromBody] CreatePreLoginRequest createPreLoginRequest)
    {
        var clientResponse = await _clientService.CheckClient(sourceClient);
        if (clientResponse.StatusCode != 200)
        {
            return Problem(detail: "Client not found", statusCode: 404);
        }
        var client = clientResponse.Response;

        if(!client!.CanCreateLoginUrl)
        {
            return Problem(detail: "Client is not authorized to use this flow", statusCode: 403);
        }

        ServiceResponse<ClientResponse> targetClientResponse;
        if (Guid.TryParse(createPreLoginRequest.clientCode, out Guid _))
        {
            targetClientResponse = await _clientService.CheckClient(createPreLoginRequest.clientCode);
        }
        else
        {
            targetClientResponse = await _clientService.CheckClientByCode(createPreLoginRequest.clientCode);
        }

        if (targetClientResponse.StatusCode != 200)
        {
            return Problem(detail: "Target client not found", statusCode: 404);
        }
        var targetClient = targetClientResponse.Response;


        if(!client.CreateLoginUrlClients!.Any(c => c.Equals(targetClient!.id)))
        {
            return Problem(detail: "Target client is not authorized to creating login url from given source client", statusCode: 403);
        }

        var user = await _userService.GetUserByReference(createPreLoginRequest.scopeUser);

        HttpContext.Session.SetString("LoggedUser", JsonSerializer.Serialize(user.Response));
        // var session = Request.Cookies[".amorphie.token"];
        // HttpContext.Response.Cookies.Append(".amorphie.token",session!);
        return Redirect(targetClient!.loginurl!);
    }

    [HttpPost("public/DodgeCreatePreLogin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IResult> DodgeCreatePreLogin([FromHeader(Name = "x-userinfo")] string userinfo, [FromBody] CreatePreLoginRequest createPreLoginRequest)
    {
        var userInfoModel = JsonSerializer.Deserialize<dynamic>(Convert.FromBase64String(userinfo));
        var usernameProperty = userInfoModel!.GetProperty("username");
        string username = usernameProperty.ToString();

        ServiceResponse<ClientResponse> targetClientResponse;
        if (Guid.TryParse(createPreLoginRequest.clientCode, out Guid _))
        {
            targetClientResponse = await _clientService.CheckClient(createPreLoginRequest.clientCode);
        }
        else
        {
            targetClientResponse = await _clientService.CheckClientByCode(createPreLoginRequest.clientCode);
        }

        if(targetClientResponse.StatusCode != 200)
        {
            return Results.Problem(detail:targetClientResponse.Detail,statusCode:targetClientResponse.StatusCode);
        }
        var targetClient = targetClientResponse.Response;

        var migrateResult = await _loginService.MigrateDodgeUserToAmorphie(username);
        if(migrateResult.StatusCode != 200)
        {
            return Results.Problem(detail:migrateResult.Detail, statusCode:migrateResult.StatusCode);
        }

        var amorphieUserResult = await _loginService.GetAmorphieUser(username);
        if(amorphieUserResult.StatusCode != 200)
        {
            return Results.Problem(detail:amorphieUserResult.Detail, statusCode:amorphieUserResult.StatusCode);
        }
        var amorphieUser = amorphieUserResult.Response;
        
        var profileResult = await _profileService.GetCustomerSimpleProfile(username);
        if(profileResult.StatusCode != 200)
        {
            return Results.Problem(detail:profileResult.Detail, statusCode:profileResult.StatusCode);
        }
        var profile = profileResult.Response;
        _logger.LogError("Profile Response : "+JsonSerializer.Serialize(profileResult));
        var authResponse = await _authorizationService.Authorize(new AuthorizationServiceRequest{
            ClientId = targetClient.id,
            RedirectUri = targetClient.returnuri,
            CodeChallange = createPreLoginRequest.CodeChallange,
            CodeChallangeMethod = "SHA256",
            Nonce = createPreLoginRequest.Nonce,
            ResponseType = "code",
            State = createPreLoginRequest.State,
            Scope = ["openid","profile"],
            User = amorphieUser,
            Profile = profile
        });

        if (authResponse.StatusCode != 200)
        {
            return Results.Problem(detail:authResponse.Detail,statusCode:authResponse.StatusCode);
        }

        return Results.Ok(new{AuthCode=authResponse.Response!.Code});
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
        string kmlkNo = string.Empty;
        if (consent.consentType.Equals("OB_Account"))
        {
            kmlkNo = consentData!.kmlk.kmlkVrs.ToString();
        }
        if (consent.consentType.Equals("OB_Payment"))
        {
            kmlkNo = consentData!.odmBsltm.kmlk.kmlkVrs.ToString();
        }

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
        ViewBag.isOn = customerInfo!.data!.profile!.businessLine;
        // if (customerInfo!.data!.profile!.businessLine == "X")
        // {
        //     return View("OpenBankingLoginOn", loginModel);
        // }
        // else
        // {
        //     return View("OpenBankingLoginBurgan", loginModel);
        // }
        return View("NewLogin", loginModel);

    }

    [HttpGet("public/Authorize")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Authorize(AuthorizationRequest authorizationRequest)
    {
        var authorize = await _authorizationService.Authorize(new AuthorizationServiceRequest
        {
            ClientId = authorizationRequest.ClientId,
            RedirectUri = authorizationRequest.RedirectUri,
            ResponseType = authorizationRequest.ResponseType,
            Scope = authorizationRequest.Scope,
            State = authorizationRequest.State
        });

        var loggedUserSerialized = HttpContext.Session.GetString("LoggedUser");
        if(string.IsNullOrWhiteSpace(loggedUserSerialized))
        {
            return View("LoginPage", new Login(){Code = authorize.Response!.Code});
        }
        else
        {
            var user = JsonSerializer.Deserialize<LoginResponse>(loggedUserSerialized);
            var profileResponse = await _profileService.GetCustomerSimpleProfile(user!.Reference);
            await _authorizationService.AssignUserToAuthorizationCode(user, authorize.Response!.Code!,profileResponse.Response!);
            return Redirect(authorize.Response!.RedirectUri);
        }

    }


    [HttpGet("public/CollectionUsers")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> CollectionUsers()
    {
        await Task.CompletedTask;
        return Ok(core.Constants.CollectionUsers.Users);
    }

    [HttpGet("public/AuthorizeCollection")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> AuthorizeCollection(AuthorizationRequest authorizationRequest)
    {
        var authorize = await _authorizationService.Authorize(new AuthorizationServiceRequest
        {
            ClientId = authorizationRequest.ClientId,
            RedirectUri = authorizationRequest.RedirectUri,
            ResponseType = authorizationRequest.ResponseType,
            Scope = authorizationRequest.Scope,
            State = authorizationRequest.State
        });



        return View("CollectionLoginPage", new Models.Account.Login(){Code = authorize.Response.Code});

    }



}
