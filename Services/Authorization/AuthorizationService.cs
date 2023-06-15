
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthServer.Enums;
using AuthServer.Exceptions;
using AuthServer.Extensions;
using AuthServer.Helpers;
using AuthServer.Models.Account;
using AuthServer.Models.Authorization;
using AuthServer.Models.Token;
using AuthServer.Models.User;
using AuthServer.Services.Client;
using Dapr.Client;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

namespace AuthServer.Services.Authorization;

public class AuthorizationService : ServiceBase,IAuthorizationService
{
    private readonly IClientService _clientService;
    private readonly DaprClient _daprClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuthorizationService(ILogger<AuthorizationService> logger,IConfiguration configuration,IClientService clientService,DaprClient daprClient,IHttpContextAccessor httpContextAccessor)
    :base(logger,configuration)
    {
        _clientService = clientService;
        _daprClient = daprClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TokenResponse> GenerateToken(TokenRequest tokenRequest)
    {
        TokenResponse tokenResponse = new();

        var client = await _clientService.ValidateClient(tokenRequest.client_id,tokenRequest.client_secret);

        if(client == null)
        {
            throw new ServiceException((int)Errors.InvalidClient,"Client Not Found");
        }

        var authorizationCodeInfo = await _daprClient.GetStateAsync<AuthorizationCode>("token-statestore",tokenRequest.code);

        if(authorizationCodeInfo == null)
        {
            throw new ServiceException((int)Errors.InvalidAuthorizationCode,"Authorization Code Not Found");
        }

        if(!authorizationCodeInfo.ClientId.Equals(tokenRequest.client_id,StringComparison.OrdinalIgnoreCase))
        {
            throw new ServiceException((int)Errors.InvalidClient,"Invalid Client");
        }
        
        if(client.pkce == "must")
        {
            if(!authorizationCodeInfo.CodeChallenge.Equals(GetHashedCodeVerifier(tokenRequest.code_verifier)))
            {
                throw new ServiceException((int)Errors.CodeVerifierNotMatched,"Code Verifier Not Matched With Code Challange");
            }
        }

        //openId Section
        if(authorizationCodeInfo.RequestedScopes.Contains("openId") || authorizationCodeInfo.RequestedScopes.Contains("profile"))
        {
            int iat = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var claims = new List<Claim>()
            {
                new Claim("iat", iat.ToString(), ClaimValueTypes.Integer),
                new Claim("nonce", authorizationCodeInfo.Nonce)
            };

            var identityInfo = client.tokens.FirstOrDefault(t => t.type == "identity");
            if(identityInfo != null)
            {
                var user = authorizationCodeInfo.Subject;
                string queryStringForTag = string.Empty;
                queryStringForTag += "?reference="+user.Reference;
                queryStringForTag += "&mail="+user.EMail;
                queryStringForTag += "&phone="+user.MobilePhone.ToString();
                foreach(var identityClaim in identityInfo.claims)
                {
                    var claimInfo = identityClaim.Split(".");
                    if(claimInfo.First().Equals("tag")){
                        try
                        {
                            var domain = claimInfo[1];
                            var entity = claimInfo[2];
                            var tagName = claimInfo[3];
                            var fieldName = claimInfo[4];

                            var tagData = await _daprClient.InvokeMethodAsync<dynamic>(HttpMethod.Get,Configuration["TagExecutionServiceAppName"],$"/tag/{domain}/{entity}/{tagName}{queryStringForTag}");
                            claims.Add(new Claim(identityClaim,tagData.fieldName));    

                        }
                        catch(InvocationException ex)
                        {
                            Logger.LogError("Dapr Service Invocation Failed | Detail:"+ex.Message);
                        }
                        catch (System.Exception ex)
                        {
                            Logger.LogError("An Error Occured When Getting Tag Data | Detail:"+ex.Message);
                        }
                    }
                    else
                    {
                        if(claimInfo.First().Equals("user"))
                        {
                            
                            Type t = user.GetType();
                            var property = t.GetProperties().First(p => p.Name.ToLower() == claimInfo[1]);
                           
                            claims.Add(new Claim(identityClaim,property.GetValue(user).ToString()));
                        }
                    }
                }
            }

            int idDuration = 0;
            try
            {
                 idDuration = TimeHelper.ConvertStrDurationToSeconds(identityInfo.duration);
            }
            catch (FormatException ex)
            {
                Logger.LogError(ex.Message);
            }
            

            var idToken = JwtHelper.GenerateJwt("Test", client.returnuri, claims,
            expires: DateTime.UtcNow.AddSeconds(idDuration));
            tokenResponse.id_token = idToken;
        }

        var tokenClaims = new List<Claim>();
        var excludedScopes = new string[]{"openId","profile"};
        foreach (var scope in tokenRequest.scopes.ToArray().Except(excludedScopes))
            tokenClaims.Add(new Claim("scope", scope));


        var accessInfo = client.tokens.FirstOrDefault(t => t.type == "access");

        int accessDuration = 0;
        try
        {
                accessDuration = TimeHelper.ConvertStrDurationToSeconds(accessInfo.duration);
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex.Message);
        }
        
        if(client.jws.mode.Equals("must"))
        {
            _httpContextAccessor.HttpContext.Response.Headers["test"] = "123";
        }

        if(accessInfo != null)
        {
            foreach(var accessClaim in accessInfo.claims)
            {
                tokenClaims.Add(new Claim(accessClaim,"123"));
            }
        }


        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["JwtSecretKey"]));
        var signinCredentials = new SigningCredentials(secretKey,SecurityAlgorithms.HmacSha384);

        string access_token = JwtHelper.GenerateJwt("Test", client.returnuri, tokenClaims,
            expires: DateTime.UtcNow.AddSeconds(accessDuration), signingCredentials:signinCredentials);

        tokenResponse.token_type = "Bearer";
        tokenResponse.access_token = access_token;
        tokenResponse.expires = accessDuration;

        return tokenResponse;
    }

    public async Task AssignUserToAuthorizationCode(LoginResponse user, string authorizationCode)
    {
        var authorizationCodeInfo = await _daprClient.GetStateAsync<AuthorizationCode>("token-statestore",authorizationCode);

        var newAuthorizationCodeInfo = authorizationCodeInfo.MapTo<AuthorizationCode>();
        newAuthorizationCodeInfo.Subject = user;

        await _daprClient.SaveStateAsync<AuthorizationCode>("token-statestore",authorizationCode,newAuthorizationCodeInfo);
    }

    public async Task<AuthorizationResponse> Authorize(AuthorizationRequest request)
    {
        AuthorizationResponse authorizationResponse = new();
        try
        {
            var client = await _clientService.CheckClient(request.client_id);

            if(client != null)
            {
                if(string.IsNullOrEmpty(request.response_type) || request.response_type != "code")
                {
                    throw new ServiceException((int)Errors.InvalidResponseType,"Response Type Parameter Has To Be Set As 'Code'");
                }

                var requestedScopes = request.scope.ToList();
                var clientScopes = client.allowedscopetags.Intersect(requestedScopes);

                if(!clientScopes.Any())
                {
                    throw new ServiceException((int)Errors.InvalidScopes,"Client is Not Authorized For Requested Scopes");
                }

                var authCode = new AuthorizationCode
                {
                    ClientId = request.client_id,
                    RedirectUri = request.redirect_uri,
                    RequestedScopes = clientScopes.ToList(),
                    CodeChallenge = request.code_challenge,
                    CodeChallengeMethod = request.code_challenge_method,
                    CreationTime = DateTime.UtcNow,
                    Subject = null,
                    Nonce = request.nonce
                };

                var code = await GenerateAuthorizationCode(authCode);

                authorizationResponse.RedirectUri = $"{client.returnuri}?response_type=code&state={request.state}";
                authorizationResponse.Code = code;
                authorizationResponse.RequestedScopes = clientScopes.ToList();
                authorizationResponse.State = request.state;

                return authorizationResponse;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
        }
        
        return authorizationResponse;
    }

    private async Task<string> GenerateAuthorizationCode(AuthorizationCode authorizationCode)
    {
        var rand = RandomNumberGenerator.Create();
        byte[] bytes = new byte[32];
        rand.GetBytes(bytes);
        var code = Base64UrlEncoder.Encode(bytes);

        await _daprClient.SaveStateAsync<AuthorizationCode>("token-statestore",code,authorizationCode);

        return code;
    }

    private string GetHashedCodeVerifier(string codeVerifier)
    {
        var codeVerifierAsByte = System.Text.Encoding.ASCII.GetBytes(codeVerifier);

        using var sha256 = SHA256.Create();
        var hashedCodeVerifier = Base64UrlEncoder.Encode(sha256.ComputeHash(codeVerifierAsByte));

        return hashedCodeVerifier;
    }
}
