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

namespace AuthServer.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserService _userService;

    public HomeController(ILogger<HomeController> logger,IAuthorizationService authorizationService,IUserService userService)
    {
        _logger = logger;
        _authorizationService = authorizationService;
        _userService = userService;
    }
    
    public  IActionResult CodeChallange(string code_verifier)
    {
        Console.WriteLine("CodeChallange Called");
        var codeVerifierAsByte = System.Text.Encoding.ASCII.GetBytes(code_verifier);

        using var sha256 = SHA256.Create();
        var hashedCodeVerifier = Base64UrlEncoder.Encode(sha256.ComputeHash(codeVerifierAsByte));
        return Content(hashedCodeVerifier);
    }

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

    [HttpPost]
    public async Task<IActionResult> Login(Login loginRequest)
    {

        var user = await _userService.Login(new LoginRequest(){Reference = loginRequest.UserName,Password = loginRequest.Password});
        if(user != null )
        {
            HttpContext.Session.SetString("LoggedUser",JsonSerializer.Serialize(user));
            await _authorizationService.AssignUserToAuthorizationCode(user,loginRequest.Code);
        
            return Redirect($"{loginRequest.RedirectUri}&code={loginRequest.Code}");
        }
        else
        {
            return Forbid();
        }        

    }

    [HttpPost]
    public async Task<IActionResult> Token(TokenRequest tokenRequest)
    {
        var token = await _authorizationService.GenerateToken(tokenRequest);
        return Json(token);
    }

    
}
