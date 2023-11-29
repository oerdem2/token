

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using amorphie.token.data;
using amorphie.token.Services.ClaimHandler;
using amorphie.token.Services.Consent;
using amorphie.token.Services.TransactionHandler;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;

namespace amorphie.token.Services.Token;

public class TokenService : ServiceBase,ITokenService
{
    private readonly IClientService _clientService;
    private readonly IUserService _userService;
    private readonly DaprClient _daprClient;
    private readonly DatabaseContext _databaseContext;
    private readonly ITransactionService _transactionService;
    private readonly IClaimHandlerService _claimService;

    private TokenInfoDetail _tokenInfoDetail;
    private GenerateTokenRequest? _tokenRequest;
    private ClientResponse? _client;
    private LoginResponse? _user;
    public TokenService(ILogger<AuthorizationService> logger, IConfiguration configuration, IClientService clientService,IClaimHandlerService claimService,
    ITransactionService transactionService, IUserService userService ,DaprClient daprClient, DatabaseContext databaseContext):base(logger,configuration)
    {
        _clientService = clientService;
        _userService = userService;
        _daprClient = daprClient;
        _databaseContext = databaseContext;
        _transactionService = transactionService;
        _claimService = claimService;

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

        if(accessTokenInfo != null)
        {
            await _daprClient.SaveStateAsync<TokenInfo>(Configuration["DAPR_STATE_STORE_NAME"], accessTokenInfo!.Id.ToString(), accessTokenInfo, metadata: new Dictionary<string, string> { { "ttlInSeconds", _tokenInfoDetail.AccessTokenDuration.ToString() } });
        }
        if(refreshTokenInfo != null)
        {
            await _daprClient.SaveStateAsync<TokenInfo>(Configuration["DAPR_STATE_STORE_NAME"], refreshTokenInfo!.Id.ToString(), refreshTokenInfo, metadata: new Dictionary<string, string> { { "ttlInSeconds", _tokenInfoDetail.RefreshTokenDuration.ToString() } });
        }
    }

    private async Task<string> CreateIdToken()
    {
        int iat = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        var claims = new List<Claim>()
        {
            new Claim("iat", iat.ToString(), ClaimValueTypes.Integer),
        };

        var identityInfo = _client!.tokens!.FirstOrDefault(t => t.type == 2);
        if(identityInfo == null)
            return string.Empty;

        if (identityInfo.claims != null && identityInfo.claims.Count() > 0)
        {
            var populatedClaims = await _claimService.PopulateClaims(identityInfo.claims);
            claims.AddRange(populatedClaims);
        }

        int idDuration = 0;
        try
        {
            idDuration = TimeHelper.ConvertStrDurationToSeconds(identityInfo.duration!);
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex.Message);
            return string.Empty;
        }

        var idToken = JwtHelper.GenerateJwt("BurganIam", _client.returnuri, claims,
        expires: DateTime.UtcNow.AddSeconds(idDuration));

        _tokenInfoDetail!.IdTokenId = Guid.NewGuid();
        _tokenInfoDetail!.TokenList.Add(JwtHelper.CreateTokenInfo(TokenType.IdToken, _tokenInfoDetail.IdTokenId, _client.id!, DateTime.UtcNow.AddSeconds(idDuration), true, _user!.Reference
        , new List<string>(), _user!.Id, _tokenInfoDetail.AccessTokenId,null));

        return idToken;
    }

    private async Task<string> CreateAccessToken()
    {
        _tokenInfoDetail.AccessTokenId = Guid.NewGuid();

        var tokenClaims = new List<Claim>();
        var excludedScopes = new string[] { "openId", "profile" };
        foreach (var scope in _tokenRequest!.Scopes!.ToArray().Except(excludedScopes))
            tokenClaims.Add(new Claim("scope", scope));


        var accessInfo = _client!.tokens!.FirstOrDefault(t => t.type == 0);
        if(accessInfo == null)
            return string.Empty;

        int accessDuration = 0;
        try
        {
            accessDuration = TimeHelper.ConvertStrDurationToSeconds(accessInfo.duration!);
            _tokenInfoDetail.AccessTokenDuration = accessDuration;
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex.Message);
            return string.Empty;
        }

        if (accessInfo.claims != null && accessInfo.claims.Count() > 0)
        {
            foreach (var accessClaim in accessInfo.claims)
            {
                var populatedClaims = await _claimService.PopulateClaims(accessInfo.claims);
                tokenClaims.AddRange(populatedClaims);
            }   
        }
        tokenClaims.Add(new Claim("jti", _tokenInfoDetail.AccessTokenId.ToString()));
        tokenClaims.Add(new Claim("userId", _user!.Id.ToString()));

        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_client.jwtSalt!));
        var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha384);

        var expires = DateTime.UtcNow.AddSeconds(accessDuration);

        string access_token = JwtHelper.GenerateJwt("BurganIam", _client.returnuri, tokenClaims,
            expires: expires, signingCredentials: signinCredentials);

        _tokenInfoDetail.TokenList.Add(JwtHelper.CreateTokenInfo(TokenType.AccessToken, _tokenInfoDetail.AccessTokenId, _client.id!, DateTime.UtcNow.AddSeconds(accessDuration), true, _user.Reference
        , _tokenRequest.Scopes!.ToArray().Except(excludedScopes).ToList(), _user.Id, null,_tokenRequest!.ConsentId));

        return access_token;
    }

    private string CreateRefreshToken()
    {
        _tokenInfoDetail.RefreshTokenId = Guid.NewGuid();
        var refreshInfo = _client!.tokens!.FirstOrDefault(t => t.type == 1);

        var tokenClaims = new List<Claim>();
        if (refreshInfo != null)
        {
            tokenClaims.Add(new Claim("jti", _tokenInfoDetail.RefreshTokenId.ToString()));
            tokenClaims.Add(new Claim("userId", _user!.Id.ToString()));
        }
        if(refreshInfo == null)
            return string.Empty;

        int refreshDuration = 0;
        try
        {
            refreshDuration = TimeHelper.ConvertStrDurationToSeconds(refreshInfo.duration!);
            _tokenInfoDetail.RefreshTokenDuration = refreshDuration;
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex.Message);
            return string.Empty;
        }

        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_client.jwtSalt!));
        var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha384);

        var refreshExpires = DateTime.UtcNow.AddSeconds(refreshDuration);
        string refresh_token = JwtHelper.GenerateJwt("BurganIam", _client.returnuri, tokenClaims,
            expires: refreshExpires, signingCredentials: signinCredentials);

        _tokenInfoDetail.TokenList.Add(JwtHelper.CreateTokenInfo(TokenType.RefreshToken, _tokenInfoDetail.RefreshTokenId, _client.id!, DateTime.UtcNow.AddSeconds(refreshDuration), true, _user!.Reference
        , new List<string>(), _user!.Id, _tokenInfoDetail.AccessTokenId,_tokenRequest!.ConsentId));

        return refresh_token;
    }

    public async Task<TokenResponse> GenerateTokenResponse()
    {
        var tokenResponse = new TokenResponse()
        {
            TokenType = "Bearer",
            AccessToken = await CreateAccessToken(),
            ExpiresIn = _tokenInfoDetail.AccessTokenDuration,
            RefreshToken = CreateRefreshToken(),
            RefreshTokenExpiresIn = _tokenInfoDetail.RefreshTokenDuration
        };

        //openId Section
        if (_tokenRequest!.Scopes!.Contains("openId") || _tokenRequest!.Scopes!.Contains("profile"))
        {
            tokenResponse.IdToken = await CreateIdToken();
        }

        return tokenResponse;
    }

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithRefreshToken(GenerateTokenRequest tokenRequest)
    {
        _tokenRequest = tokenRequest;
        
        var refreshTokenJti = JwtHelper.GetClaim(tokenRequest.RefreshToken!,"jti");
        if(refreshTokenJti == null)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 404,
                Detail = "Refresh Token Not Found",
                Response = null
            };
        }
        var refreshTokenInfo = _databaseContext.Tokens.FirstOrDefault(t =>
        t.Id == Guid.Parse(refreshTokenJti!) && t.TokenType == TokenType.RefreshToken);

        if (refreshTokenInfo == null)
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
        t.Id == refreshTokenInfo.RelatedTokenId && t.TokenType == TokenType.AccessToken);
        if (relatedToken == null)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 404,
                Detail = "Related Access Token Not Found",
                Response = null
            };
        }

        tokenRequest.Scopes = relatedToken.Scopes;

        var clientResponse = await _clientService.CheckClient(refreshTokenInfo.ClientId);

        if (clientResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = clientResponse.StatusCode,
                Detail = clientResponse.Detail
            };
        }
        _client = clientResponse.Response;

        if (!_client!.allowedgranttypes!.Any(g => g.GrantType == tokenRequest.GrantType))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Client Has No Authorize To Use Requested Grant Type"
            };
        }

        var requestedScopes = tokenRequest.Scopes.ToList();

        if (!requestedScopes.All(_client.allowedscopetags!.Contains))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 473,
                Detail = "Client is Not Authorized For Requested Scopes"
            };
        }
        
        if(refreshTokenInfo.UserId != null)
        {
            var userResponse = await _userService.GetUserById(refreshTokenInfo.UserId!.Value);

            if (userResponse.StatusCode != 200)
            {
                return new ServiceResponse<TokenResponse>()
                {
                    StatusCode = userResponse.StatusCode,
                    Detail = userResponse.Detail
                };
            }

            _user = userResponse.Response;

            if (_user?.State.ToLower() != "active" && _user?.State.ToLower() != "new")
            {
                return new ServiceResponse<TokenResponse>()
                {
                    StatusCode = 470,
                    Detail = "User is disabled"
                };
            }
        }

        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_client.jwtSalt!));
        JwtSecurityToken? refreshTokenValidated;
        if (JwtHelper.ValidateToken(tokenRequest.RefreshToken!, "BurganIam", _client.returnuri, secretKey, out refreshTokenValidated))
        {

            var tokenResponse = await GenerateTokenResponse();

            if(tokenResponse.AccessToken == string.Empty || tokenResponse.RefreshToken == string.Empty)
            {
                return new ServiceResponse<TokenResponse>()
                {
                    StatusCode = 500,
                    Detail = "Access Token Or Refresh Token Couldn't Be Generated"
                };
            }

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

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithPasswordFromWorkflow(GenerateTokenRequest tokenRequest, ClientResponse client, LoginResponse user)
    {
        _tokenRequest = tokenRequest;

        _client = client;
        _user = user;

        var tokenResponse = await GenerateTokenResponse();

        if(tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 500,
                Detail = "Access Token And Id Token Couldn't Be Generated"
            };
        }

        await PersistTokenInfo();

        return new ServiceResponse<TokenResponse>()
        {
            StatusCode = 200,
            Response = tokenResponse
        };
    }

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithPassword(GenerateTokenRequest tokenRequest)
    {
        _tokenRequest = tokenRequest;

        var clientResponse = await _clientService.ValidateClient(_tokenRequest.ClientId!, _tokenRequest.ClientSecret!);

        if (clientResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = clientResponse.StatusCode,
                Detail = clientResponse.Detail
            };
        }

        _client = clientResponse.Response;
        if (!_client!.allowedgranttypes!.Any(g => g.GrantType == tokenRequest.GrantType))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Client Has No Authorize To Use Requested Grant Type"
            };
        }

        var requestedScopes = tokenRequest.Scopes!.ToList();

        if (!requestedScopes.All(_client.allowedscopetags!.Contains))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 473,
                Detail = "Client is Not Authorized For Requested Scopes"
            };
        }

        var userResponse = await _userService.Login(new LoginRequest() { Reference = tokenRequest.Username!, Password = tokenRequest.Password! });

        if (userResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = userResponse.StatusCode,
                Detail = userResponse.Detail
            };
        }

        _user = userResponse.Response;
        await _transactionService.SaveUser(_user!);

        if (_user?.State.ToLower() != "active" && _user?.State.ToLower() != "new")
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 470,
                Detail = "User is disabled"
            };
        }

        var tokenResponse = await GenerateTokenResponse();

        if(tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 500,
                Detail = "Access Token And Id Token Couldn't Be Generated"
            };
        }

        await PersistTokenInfo();

        return new ServiceResponse<TokenResponse>()
        {
            StatusCode = 200,
            Response = tokenResponse
        };
    }

    public async Task<ServiceResponse<TokenResponse>> GenerateToken(GenerateTokenRequest tokenRequest)
    {
        _tokenRequest = tokenRequest;

        var clientResponse = await _clientService.ValidateClient(tokenRequest.ClientId!, tokenRequest.ClientSecret!);
        if (clientResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = clientResponse.StatusCode,
                Detail = clientResponse.Detail
            };
        }

        _client = clientResponse.Response;

        if (!_client!.allowedgranttypes!.Any(g => g.GrantType == tokenRequest.GrantType))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Client Has No Authorize To Use Requested Grant Type"
            };
        }

        var authorizationCodeInfo = await _daprClient.GetStateAsync<AuthorizationCode>(Configuration["DAPR_STATE_STORE_NAME"], tokenRequest.Code);

        if (authorizationCodeInfo == null)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 470,
                Detail = "Invalid Authorization Code"
            };
        }

        _user = authorizationCodeInfo.Subject;

        if (!authorizationCodeInfo.ClientId!.Equals(tokenRequest.ClientId, StringComparison.OrdinalIgnoreCase))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "ClientId Not Matched"
            };
        }

        if (_client.pkce == "must")
        {
            if (!authorizationCodeInfo.CodeChallenge!.Equals(GetHashedCodeVerifier(tokenRequest.CodeVerifier!)))
            {
                return new ServiceResponse<TokenResponse>()
                {
                    StatusCode = 472,
                    Detail = "Code Verifier Not Matched"
                };
            }
        }

        var tokenResponse = await GenerateTokenResponse();

        if(tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 500,
                Detail = "Access Token And Id Token Couldn't Be Generated"
            };
        }

        await PersistTokenInfo();
        return new ServiceResponse<TokenResponse>()
        {
            StatusCode = 200,
            Response = tokenResponse
        };
    }

    private string GetHashedCodeVerifier(string codeVerifier)
    {
        var codeVerifierAsByte = System.Text.Encoding.ASCII.GetBytes(codeVerifier);

        using var sha256 = SHA256.Create();
        var hashedCodeVerifier = Base64UrlEncoder.Encode(sha256.ComputeHash(codeVerifierAsByte));

        return hashedCodeVerifier;
    }
}
