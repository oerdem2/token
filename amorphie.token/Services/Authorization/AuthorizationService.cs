
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using amorphie.token.core.Extensions;
using Microsoft.IdentityModel.Tokens;
using amorphie.token.data;
using System.IdentityModel.Tokens.Jwt;
using amorphie.token.Services.Profile;
using Microsoft.AspNetCore.Http;

namespace amorphie.token.Services.Authorization;

public class AuthorizationService : ServiceBase, IAuthorizationService
{
    private readonly IClientService _clientService;
    private readonly ITagService _tagService;
    private readonly IUserService _userService;
    private readonly DaprClient _daprClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DatabaseContext _databaseContext;
    private readonly IProfileService _profileService;


    private TokenInfoDetail _tokenInfoDetail;

    public AuthorizationService(ILogger<AuthorizationService> logger, IConfiguration configuration, IClientService clientService, ITagService tagService,
    IUserService userService, DaprClient daprClient, IHttpContextAccessor httpContextAccessor, DatabaseContext databaseContext, IProfileService profileService)
    : base(logger, configuration)
    {
        _clientService = clientService;
        _tagService = tagService;
        _daprClient = daprClient;
        _httpContextAccessor = httpContextAccessor;
        _databaseContext = databaseContext;
        _userService = userService;
        _profileService = profileService;
        _tokenInfoDetail = new();
    }

    private async Task PersistTokenInfo()
    {
        await WriteToDb();
        await WriteToCache();
    }

    private async Task WriteToDb()
    {
        foreach (var tokenInfo in _tokenInfoDetail.TokenList)
        {
            if (tokenInfo != null)
                _databaseContext.Tokens.Add(tokenInfo);
        }
        await _databaseContext.SaveChangesAsync();
    }

    private async Task WriteToCache()
    {
        var accessTokenInfo = _tokenInfoDetail.TokenList.FirstOrDefault(t => t.TokenType == TokenType.AccessToken);
        var refreshTokenInfo = _tokenInfoDetail.TokenList.FirstOrDefault(t => t.TokenType == TokenType.RefreshToken);

        await _daprClient.SaveStateAsync<TokenInfo>(Configuration["DAPR_STATE_STORE_NAME"], "access_token_" + accessTokenInfo.Jwt, accessTokenInfo, metadata: new Dictionary<string, string> { { "ttlInSeconds", _tokenInfoDetail.AccessTokenDuration.ToString() } });
        await _daprClient.SaveStateAsync<TokenInfo>(Configuration["DAPR_STATE_STORE_NAME"], "refresh_token_" + refreshTokenInfo.Jwt, refreshTokenInfo, metadata: new Dictionary<string, string> { { "ttlInSeconds", _tokenInfoDetail.RefreshTokenDuration.ToString() } });
    }

    private async Task<string> CreateIdToken(ClientResponse client, LoginResponse user)
    {
        int iat = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        var claims = new List<Claim>()
        {
            new Claim("iat", iat.ToString(), ClaimValueTypes.Integer),
        };

        var identityInfo = client.tokens.FirstOrDefault(t => t.type == 2);
        if (identityInfo != null)
        {
            var populatedClaims = await PopulateClaims(identityInfo.claims, user);
            claims.AddRange(populatedClaims);
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

        var idToken = JwtHelper.GenerateJwt("BurganIam", client.returnuri, claims,
        expires: DateTime.UtcNow.AddSeconds(idDuration));

        _tokenInfoDetail.IdTokenId = Guid.NewGuid();
        _tokenInfoDetail.TokenList.Add(JwtHelper.CreateTokenInfo(TokenType.IdToken, _tokenInfoDetail.IdTokenId, client.id, DateTime.UtcNow.AddSeconds(idDuration), true, idToken, user.Reference
        , new List<string>(), user.Id, _tokenInfoDetail.AccessTokenId));

        return idToken;
    }

    private async Task<string> CreateAccessToken(TokenRequest tokenRequest, ClientResponse client, LoginResponse user)
    {
        _tokenInfoDetail.AccessTokenId = Guid.NewGuid();

        var tokenClaims = new List<Claim>();
        var excludedScopes = new string[] { "openId", "profile" };
        foreach (var scope in tokenRequest.scopes.ToArray().Except(excludedScopes))
            tokenClaims.Add(new Claim("scope", scope));


        var accessInfo = client.tokens.FirstOrDefault(t => t.type == 0);

        int accessDuration = 0;
        try
        {
            accessDuration = TimeHelper.ConvertStrDurationToSeconds(accessInfo.duration);
            _tokenInfoDetail.AccessTokenDuration = accessDuration;
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex.Message);
        }

        if (accessInfo != null)
        {
            foreach (var accessClaim in accessInfo.claims)
            {
                var populatedClaims = await PopulateClaims(accessInfo.claims, user);
                tokenClaims.AddRange(populatedClaims);
            }
            tokenClaims.Add(new Claim("jti", _tokenInfoDetail.AccessTokenId.ToString()));
            tokenClaims.Add(new Claim("userId", user.Id.ToString()));
        }


        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(client.jwtSalt));
        var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha384);

        var expires = DateTime.UtcNow.AddSeconds(accessDuration);

        string access_token = JwtHelper.GenerateJwt("BurganIam", client.returnuri, tokenClaims,
            expires: expires, signingCredentials: signinCredentials);

        _tokenInfoDetail.TokenList.Add(JwtHelper.CreateTokenInfo(TokenType.AccessToken, _tokenInfoDetail.AccessTokenId, client.id, DateTime.UtcNow.AddSeconds(accessDuration), true, access_token, user.Reference
        , tokenRequest.scopes.ToArray().Except(excludedScopes).ToList(), user.Id, null));

        return access_token;
    }

    private string CreateRefreshToken(ClientResponse client, LoginResponse user)
    {
        _tokenInfoDetail.RefreshTokenId = Guid.NewGuid();
        var refreshInfo = client.tokens.FirstOrDefault(t => t.type == 1);

        var tokenClaims = new List<Claim>();
        if (refreshInfo != null)
        {
            tokenClaims.Add(new Claim("jti", _tokenInfoDetail.RefreshTokenId.ToString()));
            tokenClaims.Add(new Claim("userId", user.Id.ToString()));
        }

        int refreshDuration = 0;
        try
        {
            refreshDuration = TimeHelper.ConvertStrDurationToSeconds(refreshInfo.duration);
            _tokenInfoDetail.RefreshTokenDuration = refreshDuration;
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex.Message);
        }

        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(client.jwtSalt));
        var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha384);

        var refreshExpires = DateTime.UtcNow.AddSeconds(refreshDuration);
        string refresh_token = JwtHelper.GenerateJwt("BurganIam", client.returnuri, tokenClaims,
            expires: refreshExpires, signingCredentials: signinCredentials);

        _tokenInfoDetail.TokenList.Add(JwtHelper.CreateTokenInfo(TokenType.RefreshToken, _tokenInfoDetail.RefreshTokenId, client.id, DateTime.UtcNow.AddSeconds(refreshDuration), true, refresh_token, user.Reference
        , new List<string>(), user.Id, _tokenInfoDetail.AccessTokenId));

        return refresh_token;
    }
    private async Task<Claim> GetClaimDetail(string[] claimPath, string queryStringForTag, LoginResponse user)
    {
        if (claimPath.First().Equals("tag"))
        {
            try
            {
                var domain = claimPath[1];
                var entity = claimPath[2];
                var tagName = claimPath[3];
                var fieldName = claimPath[4];

                var tagData = await _tagService.GetTagInfo(domain, entity, tagName, queryStringForTag);
                if (tagData == null)
                    return null;

                return new Claim(string.Join('.', claimPath), tagData[fieldName].ToString());

            }
            catch (Exception ex)
            {
                Logger.LogError("Get Tag Info :" + ex.ToString());
            }
        }
        else
        {
            if (claimPath.First().Equals("user"))
            {

                Type t = user.GetType();
                t.GetProperties().ToList().ForEach(e => Console.WriteLine(e.Name.ToLower()));
                var property = t.GetProperties().FirstOrDefault(p => p.Name.ToLower() == claimPath[1]);
                if (property == null)
                    return null;
                return new Claim(string.Join('.', claimPath), property.GetValue(user).ToString());
            }
        }

        return null;
    }

    private async Task<List<Claim>> PopulateClaims(List<string> clientClaims, LoginResponse user)
    {
        List<Claim> claims = new List<Claim>();

        string queryStringForTag = string.Empty;
        queryStringForTag += "?reference=" + user.Reference;
        queryStringForTag += "&mail=" + user.EMail;
        queryStringForTag += "&phone=" + user.MobilePhone.ToString();
        foreach (var identityClaim in clientClaims)
        {
            var claimDetail = identityClaim.Split("||");
            var primaryClaim = String.Empty;
            var alternativeClaim = String.Empty;

            if (claimDetail.Length == 1)
            {
                primaryClaim = claimDetail.First();
            }
            else
            {
                primaryClaim = claimDetail.First();
                alternativeClaim = claimDetail[1];
            }

            var claimInfo = primaryClaim.Split(".");

            var claimValue = await GetClaimDetail(claimInfo, queryStringForTag, user);

            if (claimValue != null)
            {
                claims.Add(claimValue);
            }
            else
            {
                claimInfo = alternativeClaim.Split(".");
                claimValue = await GetClaimDetail(claimInfo, queryStringForTag, user);
                if (claimValue != null)
                {
                    claims.Add(claimValue);
                }
            }
        }

        return claims;
    }

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithRefreshToken(TokenRequest tokenRequest)
    {

        var refreshToken = tokenRequest.refresh_token;
        var refreshTokenInfo = _databaseContext.Tokens.FirstOrDefault(t =>
        t.TokenType == TokenType.RefreshToken && t.Jwt == refreshToken);

        if (refreshToken == null)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 404,
                Detail = "Refresh Token Not Found",
                Response = null
            };
        }

        if (!refreshTokenInfo.IsActive)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 403,
                Detail = "Refresh Token is Not Valid",
                Response = null
            };
        }

        var relatedToken = _databaseContext.Tokens.FirstOrDefault(t =>
        t.TokenType == TokenType.AccessToken && t.Id == refreshTokenInfo.RelatedTokenId);
        if (relatedToken == null)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 404,
                Detail = "Related Access Token Not Found",
                Response = null
            };
        }

        tokenRequest.scopes = relatedToken.Scopes;

        var clientResponse = await _clientService.CheckClient(refreshTokenInfo.ClientId);

        if (clientResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = clientResponse.StatusCode,
                Detail = clientResponse.Detail
            };
        }
        var client = clientResponse.Response;

        if (!client.allowedgranttypes.Any(g => g.GrantType == tokenRequest.grant_type))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Client Has No Authorize To Use Requested Grant Type"
            };
        }

        var requestedScopes = tokenRequest.scopes.ToList();

        if (!requestedScopes.All(client.allowedscopetags.Contains))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 473,
                Detail = "Client is Not Authorized For Requested Scopes"
            };
        }

        var userResponse = await _userService.GetUserById(refreshTokenInfo.UserId);

        if (userResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = userResponse.StatusCode,
                Detail = userResponse.Detail
            };
        }

        var user = userResponse.Response;

        if (user?.State.ToLower() != "active" && user?.State.ToLower() != "new")
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 470,
                Detail = "User is disabled"
            };
        }

        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(client.jwtSalt));
        JwtSecurityToken refreshTokenValidated;
        if (JwtHelper.ValidateToken(relatedToken.Jwt, "BurganIam", client.returnuri, secretKey, out refreshTokenValidated))
        {

            TokenResponse tokenResponse = new();
            tokenResponse.token_type = "Bearer";
            tokenResponse.access_token = await CreateAccessToken(tokenRequest, client, user);
            tokenResponse.expires_in = _tokenInfoDetail.AccessTokenDuration;
            tokenResponse.refresh_token = CreateRefreshToken(client, user);

            refreshTokenInfo.IsActive = false;

            await PersistTokenInfo();

            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 200,
                Response = tokenResponse
            };
        }
        else
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 401,
                Detail = "Token Validation Error",
                Response = null
            };
        }

    }

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithPasswordFromWorkflow(TokenRequest tokenRequest, ClientResponse client, LoginResponse user)
    {
        TokenResponse tokenResponse = new();

        //openId Section
        if (tokenRequest.scopes.Contains("openId") || tokenRequest.scopes.Contains("profile"))
        {
            tokenResponse.id_token = await CreateIdToken(client, user);
        }

        tokenResponse.token_type = "Bearer";
        tokenResponse.access_token = await CreateAccessToken(tokenRequest, client, user);
        tokenResponse.expires_in = _tokenInfoDetail.AccessTokenDuration;
        tokenResponse.refresh_token = CreateRefreshToken(client, user);

        await PersistTokenInfo();

        return new ServiceResponse<TokenResponse>()
        {
            StatusCode = 200,
            Response = tokenResponse
        };
    }

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithPassword(TokenRequest tokenRequest)
    {
        TokenResponse tokenResponse = new();

        var clientResponse = await _clientService.ValidateClient(tokenRequest.client_id, tokenRequest.client_secret);

        if (clientResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = clientResponse.StatusCode,
                Detail = clientResponse.Detail
            };
        }
        var client = clientResponse.Response;
        if (!client.allowedgranttypes.Any(g => g.GrantType == tokenRequest.grant_type))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Client Has No Authorize To Use Requested Grant Type"
            };
        }

        var requestedScopes = tokenRequest.scopes.ToList();

        if (!requestedScopes.All(client.allowedscopetags.Contains))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 473,
                Detail = "Client is Not Authorized For Requested Scopes"
            };
        }

        var userResponse = await _userService.Login(new LoginRequest() { Reference = tokenRequest.username, Password = tokenRequest.password });

        if (userResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = userResponse.StatusCode,
                Detail = userResponse.Detail
            };
        }

        var user = userResponse.Response;

        if (user?.State.ToLower() != "active" && user?.State.ToLower() != "new")
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 470,
                Detail = "User is disabled"
            };
        }

        //openId Section
        if (tokenRequest.scopes.Contains("openId") || tokenRequest.scopes.Contains("profile"))
        {
            tokenResponse.id_token = await CreateIdToken(client, user);
        }

        tokenResponse.token_type = "Bearer";
        tokenResponse.access_token = await CreateAccessToken(tokenRequest, client, user);
        tokenResponse.expires_in = _tokenInfoDetail.AccessTokenDuration;
        tokenResponse.refresh_token = CreateRefreshToken(client, user);

        await PersistTokenInfo();

        return new ServiceResponse<TokenResponse>()
        {
            StatusCode = 200,
            Response = tokenResponse
        };
    }

    public async Task<ServiceResponse<TokenResponse>> GenerateToken(TokenRequest tokenRequest)
    {
        TokenResponse tokenResponse = new();

        var clientResponse = await _clientService.ValidateClient(tokenRequest.client_id, tokenRequest.client_secret);
        if (clientResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = clientResponse.StatusCode,
                Detail = clientResponse.Detail
            };
        }

        var client = clientResponse.Response;

        if (!client.allowedgranttypes.Any(g => g.GrantType == tokenRequest.grant_type))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Client Has No Authorize To Use Requested Grant Type"
            };
        }

        var authorizationCodeInfo = await _daprClient.GetStateAsync<AuthorizationCode>(Configuration["DAPR_STATE_STORE_NAME"], tokenRequest.code);

        if (authorizationCodeInfo == null)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 470,
                Detail = "Invalid Authorization Code"
            };
        }

        if (!authorizationCodeInfo.ClientId.Equals(tokenRequest.client_id, StringComparison.OrdinalIgnoreCase))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "ClientId Not Matched"
            };
        }

        if (client.pkce == "must")
        {
            if (!authorizationCodeInfo.CodeChallenge.Equals(GetHashedCodeVerifier(tokenRequest.code_verifier)))
            {
                return new ServiceResponse<TokenResponse>()
                {
                    StatusCode = 472,
                    Detail = "Code Verifier Not Matched"
                };
            }
        }

        //openId Section
        if (tokenRequest.scopes.Contains("openId") || tokenRequest.scopes.Contains("profile"))
        {
            tokenResponse.id_token = await CreateIdToken(client, authorizationCodeInfo.Subject);
        }

        tokenResponse.token_type = "Bearer";
        tokenResponse.access_token = await CreateAccessToken(tokenRequest, client, authorizationCodeInfo.Subject);
        tokenResponse.expires_in = _tokenInfoDetail.AccessTokenDuration;
        tokenResponse.refresh_token = CreateRefreshToken(client, authorizationCodeInfo.Subject);

        await PersistTokenInfo();
        return new ServiceResponse<TokenResponse>()
        {
            StatusCode = 200,
            Response = tokenResponse
        };
    }



    public async Task AssignUserToAuthorizationCode(LoginResponse user, string authorizationCode)
    {
        var authorizationCodeInfo = await _daprClient.GetStateAsync<AuthorizationCode>(Configuration["DAPR_STATE_STORE_NAME"], authorizationCode);

        var newAuthorizationCodeInfo = authorizationCodeInfo.MapTo<AuthorizationCode>();
        newAuthorizationCodeInfo.Subject = user;

        await _daprClient.SaveStateAsync<AuthorizationCode>(Configuration["DAPR_STATE_STORE_NAME"], authorizationCode, newAuthorizationCodeInfo);
    }

    public async Task<ServiceResponse<AuthorizationResponse>> Authorize(AuthorizationRequest request)
    {
        AuthorizationResponse authorizationResponse = new();
        try
        {
            var clientResponse = await _clientService.CheckClient(request.client_id);
            if (clientResponse.StatusCode != 200)
            {
                return new ServiceResponse<AuthorizationResponse>()
                {
                    StatusCode = clientResponse.StatusCode,
                    Detail = clientResponse.Detail
                };
            }
            var client = clientResponse.Response;

            if (string.IsNullOrEmpty(request.response_type) || request.response_type != "code")
            {
                return new ServiceResponse<AuthorizationResponse>()
                {
                    StatusCode = 473,
                    Detail = "Response Type Parameter Has To Be Set As 'Code'"
                };
            }

            var requestedScopes = request.scope.ToList();

            if (!requestedScopes.All(client.allowedscopetags.Contains))
            {
                return new ServiceResponse<AuthorizationResponse>()
                {
                    StatusCode = 473,
                    Detail = "Client is Not Authorized For Requested Scopes"
                };
            }

            var authCode = new AuthorizationCode
            {
                ClientId = request.client_id,
                RedirectUri = request.redirect_uri,
                RequestedScopes = requestedScopes,
                CodeChallenge = request.code_challenge,
                CodeChallengeMethod = request.code_challenge_method,
                CreationTime = DateTime.UtcNow,
                Subject = null,
                Nonce = request.nonce
            };

            var code = await GenerateAuthorizationCode(authCode);

            authorizationResponse.RedirectUri = $"{client.returnuri}?response_type=code&state={request.state}";
            authorizationResponse.Code = code;
            authorizationResponse.RequestedScopes = requestedScopes;
            authorizationResponse.State = request.state;

            return new ServiceResponse<AuthorizationResponse>()
            {
                StatusCode = 200,
                Response = authorizationResponse
            };

        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            return new ServiceResponse<AuthorizationResponse>()
            {
                StatusCode = 500,
                Response = null
            };
        }

    }

    private async Task<string> GenerateAuthorizationCode(AuthorizationCode authorizationCode)
    {
        var rand = RandomNumberGenerator.Create();
        byte[] bytes = new byte[32];
        rand.GetBytes(bytes);
        var code = Base64UrlEncoder.Encode(bytes);

        await _daprClient.SaveStateAsync<AuthorizationCode>(Configuration["DAPR_STATE_STORE_NAME"], code, authorizationCode);

        return code;
    }

    private string GenerateRawAuthorizationCode()
    {
        var rand = RandomNumberGenerator.Create();
        byte[] bytes = new byte[32];
        rand.GetBytes(bytes);
        var code = Base64UrlEncoder.Encode(bytes);
        return code;
    }

    private string GetHashedCodeVerifier(string codeVerifier)
    {
        var codeVerifierAsByte = System.Text.Encoding.ASCII.GetBytes(codeVerifier);

        using var sha256 = SHA256.Create();
        var hashedCodeVerifier = Base64UrlEncoder.Encode(sha256.ComputeHash(codeVerifierAsByte));

        return hashedCodeVerifier;
    }

    public async Task<ServiceResponse<OpenBankingAuthorizationResponse>> OpenBankingAuthorize(OpenBankingAuthorizationRequest request)
    {
        var response = new ServiceResponse<OpenBankingAuthorizationResponse>();

        response.Response = new OpenBankingAuthorizationResponse(){
            Code = GenerateRawAuthorizationCode()
        };
        response.StatusCode = 200;

        return response;
    }
}
