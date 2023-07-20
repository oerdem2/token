using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AuthServer.Models;
using AuthServer.Models.Authorization;
using AuthServer.Services.Authorization;
using AuthServer.Models.Account;
using System.Text.Json;
using AuthServer.Models.Token;
using AuthServer.Models.MockData;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using AuthServer.Models.User;
using AuthServer.Services.User;
using AuthServer.Exceptions;
using amorphie.token;
using token.Models.Token;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Dapr.Client;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserService _userService;
    private readonly DatabaseContext _databaseContext;
    private readonly IConfiguration _configuration;
    private readonly DaprClient _daprClient;
    public HomeController(ILogger<HomeController> logger,IAuthorizationService authorizationService,IUserService userService,DatabaseContext databaseContext
    ,IConfiguration configuration,DaprClient daprClient)
    {
        _logger = logger;
        _authorizationService = authorizationService;
        _userService = userService;
        _databaseContext = databaseContext;
        _configuration = configuration;
        _daprClient = daprClient;
    }
    
    [HttpPut("Token/Revoke/{reference}")]
    public async Task<IActionResult> Revoke(string reference)
    {
        try
        {
            await _databaseContext.Tokens.Where(t => t.Reference == reference).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive,false));
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError("Revoke Tokens Failed. Detail:"+ex.ToString());
        }
        
        return StatusCode(500); 
    }

    [HttpGet("GenerateCodeChallenge")]
    public  IActionResult CodeChallange(string code_verifier)
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
        
        if(authorizationResponse.StatusCode != 200)
        {
            return Content($"An Error Occured. Detail : "+authorizationResponse.Detail);
        }

        var authorizationResult = authorizationResponse.Response;

        if(HttpContext.Session.Get("LoggedUser") == null)
        {
            var loginModel = new Login()
            {
                Code = authorizationResult.Code,
                RedirectUri = authorizationResult.RedirectUri,
                RequestedScopes = authorizationResult.RequestedScopes
            };
            ViewBag.HasError = false;
            return View("Login",loginModel);
        }

        var loggedUser = JsonSerializer.Deserialize<LoginResponse>(HttpContext.Session.GetString("LoggedUser"));
        
        await _authorizationService.AssignUserToAuthorizationCode(loggedUser,authorizationResult.Code);

        return Redirect($"{authorizationResult.RedirectUri}&code={authorizationResult.Code}");

    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost]
    public async Task<IActionResult> Login(Login loginRequest)
    {
        
        try
        {
            if(string.IsNullOrWhiteSpace(loginRequest.UserName) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                ViewBag.HasError = true;
                ViewBag.ErrorDetail = "Reference and Password Can Not Be Empty";
            }
            var userResponse = await _userService.Login(new LoginRequest(){Reference = loginRequest.UserName,Password = loginRequest.Password});
            if(userResponse.StatusCode != 200)
            {
                ViewBag.HasError = true;
                ViewBag.ErrorDetail = userResponse.Detail;
                var loginModel = new Login()
                {
                    Code = loginRequest.Code,
                    RedirectUri = loginRequest.RedirectUri,
                    RequestedScopes = loginRequest.RequestedScopes
                };
                return View("Login",loginModel);
            }
            var user = userResponse.Response;

            if((user?.State.ToLower() == "active" || user?.State.ToLower() == "new") )
            {
                HttpContext.Session.SetString("LoggedUser",JsonSerializer.Serialize(user));
                await _authorizationService.AssignUserToAuthorizationCode(user,loginRequest.Code);
            
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
                return View("Login",loginModel);
            }        
        }
        catch (System.Exception ex)
        {
            
            throw;
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("Token")]
    public async Task<IActionResult> Token([FromBody]TokenRequest tokenRequest)
    {
        if(tokenRequest.grant_type == "authorization_code")
        {
            var token = await _authorizationService.GenerateToken(tokenRequest);
            if(token.StatusCode == 200)
            {
                return Json(token.Response);
            }
            else
            {
                return Problem(detail:token.Detail,statusCode:token.StatusCode);
            }
        }
        if(tokenRequest.grant_type == "password")
        {
            var token = await _authorizationService.GenerateTokenWithPassword(tokenRequest);
            if(token.StatusCode == 200)
            {
                return Json(token.Response);
            }
            else
            {
                return Problem(detail:token.Detail,statusCode:token.StatusCode);
            }
        }
        return Problem(detail:"Invalid Grant Type",statusCode:480);
    }


    [HttpGet("Tokens/User/{UserId}")]
    [SwaggerResponse(200, "Sms was sent successfully", typeof(List<TokenInfoDto>))]
    public async Task<IActionResult> GetTokensBelongToUser(Guid UserId)
    {
        List<TokenInfoDto> tokensBelongToUser = new List<TokenInfoDto>();
        var tokens = _databaseContext.Tokens.Where(t => t.UserId == UserId).OrderByDescending(t => t.IssuedAt);
        foreach(var token in tokens)
        {
            tokensBelongToUser.Add(new(){
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
            handler.ValidateToken(request.token,new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSecretKey"]))
            },out validatedToken);

            var tokenInfo = await _daprClient.GetStateAsync<TokenInfo>(_configuration["DAPR_STATE_STORE_NAME"],request.token);

            response.Active = true;
            response.ClientId = tokenInfo.ClientId;
            response.Scope = string.Join(" ",tokenInfo.Scopes);
            response.Reference = tokenInfo.Reference;
            response.ExpiredAt = tokenInfo.ExpiredAt;
            return Json(response);
        }
        catch (Exception ex)
        {
            response.Active = false;
            return Json(response);
        }
        
    }

    
}
