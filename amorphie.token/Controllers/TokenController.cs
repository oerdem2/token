using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using amorphie.token.data;
using amorphie.token.core.Models.Workflow;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Consent;
using amorphie.token.Services.TransactionHandler;
using amorphie.token.core.Extensions;
using System.Dynamic;
using System.Security.Claims;
using Google.Api;

namespace amorphie.token.core.Controllers;

public class TokenController : Controller
{
    private readonly ILogger<TokenController> _logger;
    private readonly ITokenService _tokenService;
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
    public TokenController(ILogger<TokenController> logger, ITokenService tokenService, IUserService userService, DatabaseContext databaseContext
    , IConfiguration configuration, DaprClient daprClient, IClientService clientService, IInternetBankingUserService ibUserService,ITransactionService transactionService,
    IFlowHandler flowHandler,IConsentService consentService,IProfileService profileService)
    {
        _logger = logger;
        _tokenService = tokenService;
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

    [HttpGet("private/signalr")]
    public async Task<IActionResult> SignalR(string reference)
    {
        await Task.CompletedTask;
        return View("SignalR");
    }

    [HttpGet("public/secured")]
    public async Task<IActionResult> secured()
    {
        await Task.CompletedTask;
        return Ok("secured");
    }

    [HttpPut("private/Revoke/{reference}")]
    public async Task<IActionResult> Revoke(string reference)
    {
        try
        {
            var tokenBelongsTouser = _databaseContext.Tokens.Where(t => t.Reference == reference);

            foreach (var token in tokenBelongsTouser)
            {
                await _daprClient.DeleteStateAsync(_configuration["DAPR_STATE_STORE_NAME"], token.Id.ToString());
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

    [HttpPut("public/Revoke/{clientId}/{reference}")]
    public async Task<IActionResult> RevokeByClient(string clientId,string reference)
    {
        try
        {
            var tokenBelongsTouser = _databaseContext.Tokens.Where(t => t.Reference == reference && t.ClientId == clientId);

            foreach (var token in tokenBelongsTouser)
            {
                await _daprClient.DeleteStateAsync(_configuration["DAPR_STATE_STORE_NAME"], token.Id.ToString());
            }

            await _databaseContext.Tokens.Where(t => t.Reference == reference).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false));


            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError("Revoke Tokens Failed. Detail:" + ex.ToString());
        }

        return StatusCode(500);
    }

    [HttpPut("private/Revoke/ConsentId/{consentId}")]
    public async Task<IActionResult> Revoke(Guid consentId)
    {
        try
        {
            var tokenBelongsToConsent = _databaseContext.Tokens.Where(t => t.ConsentId == consentId);

            foreach (var token in tokenBelongsToConsent)
            {
                await _daprClient.DeleteStateAsync(_configuration["DAPR_STATE_STORE_NAME"], token.Id.ToString());
            }

            await _databaseContext.Tokens.Where(t => t.ConsentId == consentId).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false));


            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError("Revoke Tokens Failed. Detail:" + ex.ToString());
        }

        return StatusCode(500);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Demo()
    {
        await Task.CompletedTask;
        ViewBag.tokenUrl = _configuration["tokenUrl"];
        ViewBag.callUrl = _configuration["callUrl"];
        return View();
    }

    [HttpGet("signalR")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> SignalR()
    {
        return View("SignalR");
    }

   

    [HttpPost("public/StartWorkflow")]
    public async Task<IActionResult> StartWorkflow([FromBody]dynamic loginRequest)
    {
        var transactionId = Guid.NewGuid();

        using var httpClient = new HttpClient();
        var workflowRequest = new WorkflowPostTransitionRequest
        {
            EntityData = JsonSerializer.Serialize(loginRequest),
            GetSignalRHub = true
        };

        StringContent request = new(JsonSerializer.Serialize(workflowRequest), Encoding.UTF8, "application/json");
        request.Headers.Add("User", Guid.NewGuid().ToString());
        request.Headers.Add("Behalf-Of-User", Guid.NewGuid().ToString());

        var httpResponse = await httpClient.PostAsync(_configuration["workflowPostTransitionUri"]!.Replace("{{recordId}}", loginRequest.GetProperty("transaction_id").ToString()), request);

        if (httpResponse.IsSuccessStatusCode)
        {
            var workflowResponse = await httpResponse.Content.ReadFromJsonAsync<WorkflowPostTransitionResponse>();
            return Ok(workflowResponse.Result);
        }
        else
        {
            return Problem(detail: "Workflow Error", statusCode: 500);
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("public/Flow")]
    public async Task<IActionResult> TokenWorkflow([FromBody] TokenRequest tokenRequest)
    {
        var clientReponse = await _clientService.ValidateClient(tokenRequest.ClientId!, tokenRequest.ClientSecret!);
        if (clientReponse.StatusCode != 200)
        {
            return Problem(detail: clientReponse.Detail, statusCode: clientReponse.StatusCode);
        }

        var client = clientReponse.Response;
        var flowType = client!.flows!.FirstOrDefault(f => f.type.ToLower().Equals("login"));

        using var httpClient = new HttpClient();
        var workflowRequest = new WorkflowPostTransitionRequest
        {
            EntityData = JsonSerializer.Serialize(tokenRequest),
            GetSignalRHub = true
        };

        StringContent request = new(JsonSerializer.Serialize(workflowRequest), Encoding.UTF8, "application/json");
        request.Headers.Add("User", Guid.NewGuid().ToString());
        request.Headers.Add("Behalf-Of-User", Guid.NewGuid().ToString());

        var httpResponse = await httpClient.PostAsync(_configuration["workflowPostTransitionUri"]!.Replace("{{recordId}}", tokenRequest.RecordId), request);

        if (httpResponse.IsSuccessStatusCode)
        {
            var workflowResponse = await httpResponse.Content.ReadFromJsonAsync<WorkflowPostTransitionResponse>();
            return Ok(workflowResponse!.Result);
        }
        else
        {
            return Problem(detail: "Workflow Error", statusCode: clientReponse.StatusCode);
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("private/Introspect")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IResult> Introspect([FromForm] string token,[FromForm] string authorization_params)
    {
        Console.WriteLine("Authorization params:"+authorization_params);
        foreach(var h in HttpContext.Request.Headers)
        {
            Console.WriteLine($"Header Key:{h.Key}  Header Value:{h.Value}");
        }

        var jti = JwtHelper.GetClaim(token, "jti");

        if (jti == null)
            return Results.Json(new { active = false });

        if (!Guid.TryParse(jti, out Guid checkedJti))
            return Results.Json(new { active = false });

        var accessTokenInfo = _databaseContext.Tokens.FirstOrDefault(t => t.Id == Guid.Parse(jti));
        if (accessTokenInfo == null)
            return Results.Json(new { active = false });
        if (accessTokenInfo.TokenType != TokenType.AccessToken || !accessTokenInfo.IsActive)
            return Results.Json(new { active = false });

        var clientInfo = await _clientService.CheckClient(accessTokenInfo.ClientId);
        var client = clientInfo.Response;

        if (client == null)
        {
            return Results.Json(new { active = false });
        }

        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(client.jwtSalt!));

        if (!JwtHelper.ValidateToken(token, "BurganIam", client!.returnuri, secretKey, out JwtSecurityToken? validatedToken))
        {
            return Results.Json(new { active = false });
        }
        
        Dictionary<string,object> claimValues = new();
        foreach(Claim claim in validatedToken!.Claims)
        {
            claimValues.Add(claim.Type.Replace(".","_"),claim.Value);
        }
        claimValues.Add("clientId",client.id!);
        claimValues.Add("active",true);
        return Results.Json(claimValues);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("public/Token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest tokenRequest)
    {
        string? xforwardedfor = HttpContext.Request.Headers.ContainsKey("X-Forwarded-For") ? HttpContext.Request.Headers.FirstOrDefault(h => h.Key.ToLower().Equals("x-forwarded-for")).Value.ToString() : null;
        var ipAddress = xforwardedfor?.Split(",")[0].Trim() ?? "undefined";
        _transactionService.IpAddress = ipAddress;
        foreach (var item in HttpContext.Request.Headers)
        {
            Console.WriteLine($"Key:{item.Key} | Value:{item.Value}");
        }
        var generateTokenRequest = tokenRequest.MapTo<GenerateTokenRequest>();
        if (tokenRequest.GrantType == "authorization_code")
        {
            var token = await _tokenService.GenerateToken(generateTokenRequest);
            if (token.StatusCode == 200)
            {
                return Json(token.Response);
            }
            else
            {
                return Problem(detail: token.Detail, statusCode: token.StatusCode);
            }
        }
        if (tokenRequest.GrantType == "password")
        {
            var token = await _tokenService.GenerateTokenWithPassword(generateTokenRequest);
            if (token.StatusCode == 200)
            {
                return Json(token.Response);
            }
            else
            {
                return Problem(detail: token.Detail, statusCode: token.StatusCode);
            }
        }

        if (tokenRequest.GrantType == "refresh_token")
        {
            var token = await _tokenService.GenerateTokenWithRefreshToken(generateTokenRequest);
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


    [HttpGet("private/Token/User/{UserId}")]
    [SwaggerResponse(200, "Sms was sent successfully", typeof(List<TokenInfoDto>))]
    public async Task<IActionResult> GetTokensBelongToUser(Guid UserId)
    {
        List<TokenInfoDto> tokensBelongToUser = new List<TokenInfoDto>();
        var tokens = await _databaseContext.Tokens.Where(t => t.UserId == UserId).OrderByDescending(t => t.IssuedAt).ToListAsync();
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


    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("public/OpenBankingToken")]
    public async Task<IActionResult> OpenBankingToken([FromBody] OpenBankingTokenRequest openBankingTokenRequest)
    {
        var generateTokenRequest = new GenerateTokenRequest();
        
        var clientResult = await _clientService.CheckClient(_configuration["OpenBankingClientId"]!);
        if(clientResult.StatusCode != 200)
        {
            return BadRequest();
        }
        var client = clientResult.Response;

        if(openBankingTokenRequest.AuthType!.Equals("yet_kod"))
        {
            generateTokenRequest.GrantType = "authorization_code";
            generateTokenRequest.ClientId = client!.id;
            generateTokenRequest.ClientSecret = client.clientsecret;
            generateTokenRequest.GrantType = "authorization_code";
            generateTokenRequest.Scopes = new List<string>(){"open-banking"};
            generateTokenRequest.Code = openBankingTokenRequest.AuthCode;

            var token = await _tokenService.GenerateToken(generateTokenRequest);
            if(token.StatusCode != 200)
            {
                return Problem(statusCode:token.StatusCode,detail:token.Detail);
            }

            var openBankingTokenResponse = new OpenBankingTokenResponse
            {
                AccessToken = token.Response!.AccessToken,
                ExpiresIn = token.Response.ExpiresIn,
                RefreshToken = token.Response.RefreshToken,
                RefreshTokenExpiresIn = token.Response.RefreshTokenExpiresIn
            };
            await _daprClient.DeleteStateAsync(_configuration["DAPR_STATE_STORE_NAME"],openBankingTokenRequest.AuthCode);
            await _daprClient.DeleteStateAsync(_configuration["DAPR_STATE_STORE_NAME"],"AuthCodeInfo_"+openBankingTokenRequest.ConsentNo);

            await _consentService.UpdateConsentForUsage(Guid.Parse(openBankingTokenRequest.ConsentNo!));
            return Ok(openBankingTokenResponse);
        }
        if(openBankingTokenRequest.AuthType.Equals("yenileme_belirteci"))
        {
            generateTokenRequest.GrantType = "refresh_token";
            generateTokenRequest.RefreshToken = openBankingTokenRequest.RefreshToken;

            var token = await _tokenService.GenerateTokenWithRefreshToken(generateTokenRequest);
            if(token.StatusCode != 200)
            {
                return Problem(statusCode:token.StatusCode,detail:token.Detail);
            }

            var openBankingTokenResponse = new OpenBankingTokenResponse
            {
                AccessToken = token.Response!.AccessToken,
                ExpiresIn = token.Response.ExpiresIn,
                RefreshToken = token.Response.RefreshToken,
                RefreshTokenExpiresIn = token.Response.RefreshTokenExpiresIn
            };
            return Ok(openBankingTokenResponse);
        }

        return BadRequest();
    }


}
