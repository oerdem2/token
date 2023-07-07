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

namespace AuthServer.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserService _userService;
    private readonly DatabaseContext _databaseContext;
    public HomeController(ILogger<HomeController> logger,IAuthorizationService authorizationService,IUserService userService,DatabaseContext databaseContext)
    {
        _logger = logger;
        _authorizationService = authorizationService;
        _userService = userService;
        _databaseContext = databaseContext;
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
        
        if(HttpContext.Session.Get("LoggedUser") == null)
        {
            var loginModel = new Login()
            {
                Code = authorizationResponse.Code,
                RedirectUri = authorizationResponse.RedirectUri,
                RequestedScopes = authorizationResponse.RequestedScopes
            };
            return View("Login",loginModel);
        }

        var loggedUser = JsonSerializer.Deserialize<LoginResponse>(HttpContext.Session.GetString("LoggedUser"));
        
        await _authorizationService.AssignUserToAuthorizationCode(loggedUser,authorizationResponse.Code);

        return Redirect($"{authorizationResponse.RedirectUri}&code={authorizationResponse.Code}");

    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost]
    public async Task<IActionResult> Login(Login loginRequest)
    {
        
        try
        {
            var user = await _userService.Login(new LoginRequest(){Reference = loginRequest.UserName,Password = loginRequest.Password});
            if(user != null && (user?.State.ToLower() == "active" || user?.State.ToLower() == "new") )
            {
                HttpContext.Session.SetString("LoggedUser",JsonSerializer.Serialize(user));
                await _authorizationService.AssignUserToAuthorizationCode(user,loginRequest.Code);
            
                return Redirect($"{loginRequest.RedirectUri}&code={loginRequest.Code}");
            }
            else
            {
                return Unauthorized();
            }        
        }
        catch (ServiceException ex)
        {
            ViewBag["Error"] = true;
            ViewBag["ErrorDetail"] = "Kullanıcı Bulunamadı.";
            return View("Login");
        }
        catch (System.Exception ex)
        {
            
            throw;
        }
        
        return Unauthorized();
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost]
    public async Task<IActionResult> Token(TokenRequest tokenRequest)
    {
        var token = await _authorizationService.GenerateToken(tokenRequest);
        return Json(token);
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

    
}
