using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using amorphie.token.data;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Consent;
using amorphie.token.Services.TransactionHandler;
using amorphie.token.core.Extensions;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using MongoDB.Bson.IO;
using Newtonsoft.Json;


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
    , IConfiguration configuration, DaprClient daprClient, IClientService clientService, IInternetBankingUserService ibUserService, ITransactionService transactionService,
    IFlowHandler flowHandler, IConsentService consentService, IProfileService profileService)
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

    [HttpPut("public/Forget/{clientId}")]
    public async Task<IActionResult> ForgetUser(string clientId, string reference)
    {
        try
        {
            await _userService.RemoveDevice(reference, clientId);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError("Remove Device Failed. Detail:" + ex.ToString());
        }

        return StatusCode(500);
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
    public async Task<IActionResult> RevokeByClient(string clientId, string reference)
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



    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("private/Introspect")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IResult> Introspect([FromForm] string token, [FromQuery] bool isTemporary = false)
    {
        
        var temporary = JwtHelper.GetClaim(token, "isTemporary");

        if (temporary != null && temporary.Equals("1"))
        {
            if (!isTemporary)
            {
                return Results.Json(new { active = false });
            }
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

        ServiceResponse<ClientResponse> clientInfo;
        if (Guid.TryParse(accessTokenInfo.ClientId, out Guid _))
        {
            clientInfo = await _clientService.CheckClient(accessTokenInfo.ClientId);
        }
        else
        {
            clientInfo = await _clientService.CheckClientByCode(accessTokenInfo.ClientId);
        }

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

        Dictionary<string, object> claimValues = new();
        foreach (Claim claim in validatedToken!.Claims)
        {
            if (!claimValues.ContainsKey(claim.Type))
            {
                if (validatedToken!.Claims.Count(c => c.Type == claim.Type) > 1)
                {
                    claimValues.Add(claim.Type.Replace(".", "_"), validatedToken!.Claims.Where(c => c.Type == claim.Type).Select(c => c.Value));
                }
                else
                {
                    if (!claim.Type.Equals("exp") && !claim.Type.Equals("nbf") && !claim.Type.Equals("iat"))
                        claimValues.Add(claim.Type.Replace(".", "_"), claim.Value);
                    else
                        claimValues.Add(claim.Type.Replace(".", "_"), long.Parse(claim.Value));
                }
            }

        }

        var privateClaims = await _daprClient.GetStateAsync<Dictionary<string,string>>(_configuration["DAPR_STATE_STORE_NAME"], $"{accessTokenInfo.Id.ToString()}_privateClaims");
        if(privateClaims is not null && privateClaims.Count() > 0)
        {
            foreach (var claim in privateClaims)
            {
                if (!claimValues.ContainsKey(claim.Key))
                {
                    if (validatedToken!.Claims.Count(c => c.Type == claim.Key) > 1)
                    {
                        claimValues.Add(claim.Key.Replace(".", "_"), validatedToken!.Claims.Where(c => c.Type == claim.Key).Select(c => c.Value));
                    }
                    else
                    {
                        if (!claim.Key.Equals("exp") && !claim.Key.Equals("nbf") && !claim.Key.Equals("iat"))
                            claimValues.Add(claim.Key.Replace(".", "_"), claim.Value);
                        else
                            claimValues.Add(claim.Key.Replace(".", "_"), long.Parse(claim.Value));
                    }
                }
            }
        }

        if (!claimValues.ContainsKey("client_id"))
            claimValues.Add("client_id", client.code ?? client.id!);
        if (!claimValues.ContainsKey("clientId"))
            claimValues.Add("clientId", client.code ?? client.id!);
        if (!claimValues.ContainsKey("clientIdReal"))
            claimValues.Add("clientIdReal", client.id!);
        claimValues["aud"] = new List<string>() { "BackOfficeApi", "WorkflowApi", "RetailLoanApi", "AutoQueryApi", "CardApi", "IntegrationLegacyApi", "CallCenterApi", "IbGwApi", "Apisix", "ScheduleApi", "TransactionApi", "IProvisionApi", "EndorsementApi", "QuerynetApi" };
        claimValues.Add("active", true);
        return Results.Json(claimValues);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("public/Token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest tokenRequest)
    {
        string? xforwardedfor = HttpContext.Request.Headers.ContainsKey("X-Forwarded-For") ? HttpContext.Request.Headers.FirstOrDefault(h => h.Key.ToLower().Equals("x-forwarded-for")).Value.ToString() : HttpContext.Connection.RemoteIpAddress?.ToString();
        var ipAddress = xforwardedfor?.Split(",")[0].Trim() ?? xforwardedfor;
        _transactionService.IpAddress = ipAddress;

        var generateTokenRequest = tokenRequest.MapTo<GenerateTokenRequest>();
        if (tokenRequest.GrantType == "device")
        {
            var token = await _tokenService.GenerateTokenWithDevice(generateTokenRequest);
            if (token.StatusCode == 200)
            {
                return Json(token.Response);
            }
            else
            {
                return Problem(detail: token.Detail, statusCode: token.StatusCode);
            }
        }
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
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!.Equals("Prod"))
                return StatusCode(403);

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

        if (tokenRequest.GrantType == "client_credentials")
        {
            var token = await _tokenService.GenerateTokenWithClientCredentials(generateTokenRequest);
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

    [HttpGet("private/CheckScope/{reference}/{scope}")]
    [SwaggerResponse(200, "Check Token Authorize")]
    public async Task<IActionResult> CheckScope(string reference, string scope, [FromHeader(Name = "scope")] string[] scopes)
    {
        if (!scopes.Contains(scope))
        {
            return StatusCode(401);
        }
        else
            return Ok();
    }

    [HttpGet("public/Logon/{clientId}/{reference}")]
    [SwaggerResponse(200, "Logons Returned Successfully", typeof(LogonDto))]
    public async Task<IActionResult> GetLastLogons(string clientId, string reference)
    {

        var lastSuccessfulLogon = await _databaseContext.Logon.OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync(l => l.ClientId.Equals(clientId) && l.Reference.Equals(reference));
        var lastFailedLogon = await _databaseContext.FailedLogon.OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync(l => l.ClientId.Equals(clientId) && l.Reference.Equals(reference));

        return Ok(new LogonDto
        {
            LastSuccessfullLogonDate = lastSuccessfulLogon?.CreatedAt,
            LastFailedLogonDate = lastFailedLogon?.CreatedAt
        });
    }

    [HttpGet("public/Logons/{clientId}/{reference}")]
    [SwaggerResponse(200, "Logons Returned Successfully", typeof(LogonDto))]
    public async Task<IActionResult> GetLastLogonsList(string clientId, string reference, int page = 0, int pageSize = 20)
    {
        var lastSuccessLogon = await _databaseContext.Logon.OrderByDescending(l => l.CreatedAt).Where(l => l.ClientId.Equals(clientId) && l.Reference.Equals(reference) && l.LogonStatus == LogonStatus.Completed).Select(l => new LogonDetailDto { LogonDate = l.CreatedAt, Channel = "ON Mobil", Status = 1 }).ToListAsync();
        var lastFailedLogon = await _databaseContext.FailedLogon.OrderByDescending(l => l.CreatedAt).Where(l => l.ClientId.Equals(clientId) && l.Reference.Equals(reference)).Select(l => new LogonDetailDto { LogonDate = l.CreatedAt, Channel = "ON Mobil", Status = 0 }).ToListAsync();
        lastFailedLogon.AddRange(lastSuccessLogon);
        lastFailedLogon = lastFailedLogon.OrderByDescending(l => l.LogonDate).Skip(page * pageSize).Take(pageSize).ToList();
        return Ok(lastFailedLogon);
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
        var consent = await _consentService.GetConsent(Guid.Parse(openBankingTokenRequest.ConsentNo!));
        var clientResult = await _clientService.CheckClient(_configuration["OpenBankingClientId"]!);
        if (clientResult.StatusCode != 200)
        {
            return BadRequest();
        }
        var client = clientResult.Response;

        if (openBankingTokenRequest.AuthType!.Equals("yet_kod"))
        {
            generateTokenRequest.GrantType = "authorization_code";
            generateTokenRequest.ClientId = client!.id;
            generateTokenRequest.ClientSecret = client.clientsecret;
            generateTokenRequest.GrantType = "authorization_code";
            generateTokenRequest.Scopes = new List<string>() { "open-banking" };
            generateTokenRequest.Code = openBankingTokenRequest.AuthCode;
            generateTokenRequest.ConsentId = consent.Response?.id;

            var token = await _tokenService.GenerateOpenBankingToken(generateTokenRequest, consent.Response);
            if (token.StatusCode != 200)
            {
                return Problem(statusCode: token.StatusCode, detail: token.Detail);
            }

            var openBankingTokenResponse = new OpenBankingTokenResponse
            {
                AccessToken = token.Response!.AccessToken,
                ExpiresIn = token.Response.ExpiresIn,
                RefreshToken = token.Response.RefreshToken,
                RefreshTokenExpiresIn = token.Response.RefreshTokenExpiresIn
            };
            await _daprClient.DeleteStateAsync(_configuration["DAPR_STATE_STORE_NAME"], openBankingTokenRequest.AuthCode);
            await _daprClient.DeleteStateAsync(_configuration["DAPR_STATE_STORE_NAME"], "AuthCodeInfo_" + openBankingTokenRequest.ConsentNo);

            await _consentService.UpdateConsentForUsage(Guid.Parse(openBankingTokenRequest.ConsentNo!));


            var requestId = Request.Headers.FirstOrDefault(h => h.Key.Equals("x-request-id"));
            var groupId = Request.Headers.FirstOrDefault(h => h.Key.Equals("x-group-id"));
            var aspspCode = Request.Headers.FirstOrDefault(h => h.Key.Equals("x-aspsp-code"));
            var tppCode = Request.Headers.FirstOrDefault(h => h.Key.Equals("x-tpp-code"));


            HttpContext.Response.Headers.Add("X-Request-ID", string.IsNullOrWhiteSpace(requestId.Value) ? Guid.NewGuid().ToString() : requestId.Value);
            HttpContext.Response.Headers.Add("X-Group-ID", string.IsNullOrWhiteSpace(groupId.Value) ? Guid.NewGuid().ToString() : groupId.Value);
            HttpContext.Response.Headers.Add("X-ASPSP-Code", string.IsNullOrWhiteSpace(aspspCode.Value) ? Guid.NewGuid().ToString() : aspspCode.Value);
            HttpContext.Response.Headers.Add("X-TPP-Code", string.IsNullOrWhiteSpace(tppCode.Value) ? Guid.NewGuid().ToString() : tppCode.Value);

            SignatureHelper.SetXJwsSignatureHeader(HttpContext, _configuration, openBankingTokenResponse);

            return Ok(openBankingTokenResponse);
        }
        if (openBankingTokenRequest.AuthType.Equals("yenileme_belirteci"))
        {
            generateTokenRequest.GrantType = "refresh_token";
            generateTokenRequest.RefreshToken = openBankingTokenRequest.RefreshToken;

            var token = await _tokenService.GenerateTokenWithRefreshToken(generateTokenRequest);
            if (token.StatusCode != 200)
            {
                return Problem(statusCode: token.StatusCode, detail: token.Detail);
            }

            var openBankingTokenResponse = new OpenBankingTokenResponse
            {
                AccessToken = token.Response!.AccessToken,
                ExpiresIn = token.Response.ExpiresIn,
                RefreshToken = token.Response.RefreshToken,
                RefreshTokenExpiresIn = token.Response.RefreshTokenExpiresIn
            };
            var requestId = Request.Headers.FirstOrDefault(h => h.Key.Equals("x-request-id"));
            var groupId = Request.Headers.FirstOrDefault(h => h.Key.Equals("x-group-id"));
            var aspspCode = Request.Headers.FirstOrDefault(h => h.Key.Equals("x-aspsp-code"));
            var tppCode = Request.Headers.FirstOrDefault(h => h.Key.Equals("x-tpp-code"));

            HttpContext.Response.Headers.Add("X-Request-ID", string.IsNullOrWhiteSpace(requestId.Value) ? Guid.NewGuid().ToString() : requestId.Value);
            HttpContext.Response.Headers.Add("X-Group-ID", string.IsNullOrWhiteSpace(groupId.Value) ? Guid.NewGuid().ToString() : groupId.Value);
            HttpContext.Response.Headers.Add("X-ASPSP-Code", string.IsNullOrWhiteSpace(aspspCode.Value) ? Guid.NewGuid().ToString() : aspspCode.Value);
            HttpContext.Response.Headers.Add("X-TPP-Code", string.IsNullOrWhiteSpace(tppCode.Value) ? Guid.NewGuid().ToString() : tppCode.Value);

            SignatureHelper.SetXJwsSignatureHeader(HttpContext, _configuration, openBankingTokenResponse);
            return Ok(openBankingTokenResponse);
        }

        return BadRequest();
    }




}
