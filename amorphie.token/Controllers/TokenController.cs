using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using amorphie.token.data;
using Login = amorphie.token.core.Models.Account.Login;
using amorphie.token.core.Models.Workflow;
using System.Runtime.CompilerServices;

namespace amorphie.token.core.Controllers;

public class TokenController : Controller
{
    private readonly ILogger<TokenController> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserService _userService;
    private readonly IClientService _clientService;
    private readonly DatabaseContext _databaseContext;
    private readonly IConfiguration _configuration;
    private readonly DaprClient _daprClient;
    public TokenController(ILogger<TokenController> logger, IAuthorizationService authorizationService, IUserService userService, DatabaseContext databaseContext
    , IConfiguration configuration, DaprClient daprClient, IClientService clientService)
    {
        _logger = logger;
        _authorizationService = authorizationService;
        _userService = userService;
        _databaseContext = databaseContext;
        _configuration = configuration;
        _daprClient = daprClient;
        _clientService = clientService;
    }



    [HttpPut("Token/Revoke/{reference}")]
    public async Task<IActionResult> Revoke(string reference)
    {
        try
        {
            var tokenBelongsTouser = _databaseContext.Tokens.Where(t => t.Reference == reference);

            foreach (var token in tokenBelongsTouser)
            {
                await _daprClient.DeleteStateAsync(_configuration["DAPR_STATE_STORE_NAME"], token.Jwt);
            }

            await _databaseContext.Tokens.Where(t => t.Reference == reference).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false));


            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError("Revoke Tokens Failed. Detail:" + ex.ToString());
        }

        return StatusCode(500);
    }

    [HttpGet("GenerateCodeChallenge")]
    public IActionResult CodeChallange(string code_verifier)
    {
        var codeVerifierAsByte = System.Text.Encoding.ASCII.GetBytes(code_verifier);

        using var sha256 = SHA256.Create();
        var hashedCodeVerifier = Base64UrlEncoder.Encode(sha256.ComputeHash(codeVerifierAsByte));
        return Content(hashedCodeVerifier);
    }


    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Authorize(AuthorizationRequest authorizationRequest)
    {

        var authorizationResponse = await _authorizationService.Authorize(authorizationRequest);

        if (authorizationResponse.StatusCode != 200)
        {
            return Content($"An Error Occured. Detail : " + authorizationResponse.Detail);
        }

        var authorizationResult = authorizationResponse.Response;

        if (HttpContext.Session.Get("LoggedUser") == null)
        {
            var loginModel = new Login()
            {
                Code = authorizationResult.Code,
                RedirectUri = authorizationResult.RedirectUri,
                RequestedScopes = authorizationResult.RequestedScopes
            };
            ViewBag.HasError = false;
            return View("Login", loginModel);
        }

        var loggedUser = JsonSerializer.Deserialize<LoginResponse>(HttpContext.Session.GetString("LoggedUser"));

        await _authorizationService.AssignUserToAuthorizationCode(loggedUser, authorizationResult.Code);

        return Redirect($"{authorizationResult.RedirectUri}&code={authorizationResult.Code}");

    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("/Token/Flow")]
    public async Task<IActionResult> TokenWorkflow([FromBody] TokenRequest tokenRequest)
    {
        var clientReponse = await _clientService.ValidateClient(tokenRequest.client_id, tokenRequest.client_secret);
        if (clientReponse.StatusCode != 200)
        {
            return Problem(detail: clientReponse.Detail, statusCode: clientReponse.StatusCode);
        }

        var client = clientReponse.Response;
        var flowType = client.flows.FirstOrDefault(f => f.type.ToLower().Equals("login"));

        using var httpClient = new HttpClient();
        var workflowRequest = new WorkflowPostTransitionRequest();
        workflowRequest.EntityData = JsonSerializer.Serialize(tokenRequest);
        workflowRequest.GetSignalRHub = true;

        StringContent request = new(JsonSerializer.Serialize(workflowRequest), Encoding.UTF8, "application/json");
        request.Headers.Add("User", Guid.NewGuid().ToString());
        request.Headers.Add("Behalf-Of-User", Guid.NewGuid().ToString());

        var httpResponse = await httpClient.PostAsync(_configuration["workflowPostTransitionUri"].Replace("{{recordId}}", tokenRequest.record_id), request);

        if (httpResponse.IsSuccessStatusCode)
        {
            var workflowResponse = await httpResponse.Content.ReadFromJsonAsync<WorkflowPostTransitionResponse>();
            return Ok(workflowResponse.Result);
        }
        else
        {
            return Problem(detail: "Workflow Error", statusCode: clientReponse.StatusCode);
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost]
    public async Task<IActionResult> Login(Login loginRequest)
    {

        try
        {
            if (string.IsNullOrWhiteSpace(loginRequest.UserName) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                ViewBag.HasError = true;
                ViewBag.ErrorDetail = "Reference and Password Can Not Be Empty";
            }
            var userResponse = await _userService.Login(new LoginRequest() { Reference = loginRequest.UserName, Password = loginRequest.Password });
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
                await _authorizationService.AssignUserToAuthorizationCode(user, loginRequest.Code);

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
        catch (System.Exception ex)
        {

            throw;
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("Token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest tokenRequest)
    {
        if (tokenRequest.grant_type == "authorization_code")
        {
            var token = await _authorizationService.GenerateToken(tokenRequest);
            if (token.StatusCode == 200)
            {
                return Json(token.Response);
            }
            else
            {
                return Problem(detail: token.Detail, statusCode: token.StatusCode);
            }
        }
        if (tokenRequest.grant_type == "password")
        {
            var token = await _authorizationService.GenerateTokenWithPassword(tokenRequest);
            if (token.StatusCode == 200)
            {
                return Json(token.Response);
            }
            else
            {
                return Problem(detail: token.Detail, statusCode: token.StatusCode);
            }
        }
        return Problem(detail: "Invalid Grant Type", statusCode: 480);
    }


    [HttpGet("Tokens/User/{UserId}")]
    [SwaggerResponse(200, "Sms was sent successfully", typeof(List<TokenInfoDto>))]
    public async Task<IActionResult> GetTokensBelongToUser(Guid UserId)
    {
        List<TokenInfoDto> tokensBelongToUser = new List<TokenInfoDto>();
        var tokens = _databaseContext.Tokens.Where(t => t.UserId == UserId).OrderByDescending(t => t.IssuedAt);
        foreach (var token in tokens)
        {
            tokensBelongToUser.Add(new()
            {
                ClientId = token.ClientId,
                ExpiredAt = token.ExpiredAt,
                IsActive = token.IsActive,
                IssuedAt = token.IssuedAt,
                Reference = token.Reference,
                Scopes = token.Scopes,
                UserId = token.UserId
            });
        }

        return Json(tokens);
    }

    [HttpPost("TokenInfo")]
    [SwaggerResponse(200, "Token is Valid", typeof(List<TokenInfoResponse>))]
    public async Task<IActionResult> GetTokenInfo([FromBody] TokenInfoRequest request)
    {
        TokenInfoResponse response = new();

        JwtSecurityTokenHandler handler = new();
        SecurityToken validatedToken;
        try
        {
            handler.ValidateToken(request.token, new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidIssuers = _configuration.GetSection("ValidIssuers").Get<IEnumerable<string>>(),
                ValidAudiences = _configuration.GetSection("ValidAudiences").Get<IEnumerable<string>>(),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSecretKey"]))
            }, out validatedToken);

            var tokenInfo = await _databaseContext.Tokens.FirstOrDefaultAsync(t => t.Jwt == request.token);

            if (tokenInfo != null && tokenInfo.IsActive)
            {
                response.Active = true;
                response.ClientId = tokenInfo.ClientId;
                response.Scope = string.Join(" ", tokenInfo.Scopes);
                response.Reference = tokenInfo.Reference;
                response.ExpiredAt = tokenInfo.ExpiredAt;
                return Json(response);
            }
            else
            {
                response.Active = false;
                return Json(response);
            }


        }
        catch (Exception ex)
        {
            response.Active = false;
            return Json(response);
        }

    }


}
