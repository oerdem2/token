

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using amorphie.token.core.Models.Consent;
using amorphie.token.core.Models.Profile;
using amorphie.token.core.Models.Role;
using amorphie.token.data;
using amorphie.token.Services.ClaimHandler;
using amorphie.token.Services.Consent;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;
using amorphie.token.Services.Role;
using amorphie.token.Services.TransactionHandler;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace amorphie.token.Services.Token;

public class TokenService(ILogger<AuthorizationService> logger, IConfiguration configuration, IClientService clientService, IClaimHandlerService claimService,
ITransactionService transactionService, IRoleService roleService, IbDatabaseContextMordor ibContextMordor, IConsentService consentService, IUserService userService, DaprClient daprClient, DatabaseContext databaseContext, IbSecurityDatabaseContext securityContext
    , IInternetBankingUserService internetBankingUserService, IProfileService profileService, IbDatabaseContext ibContext) : ServiceBase(logger, configuration), ITokenService
{
    private readonly IClientService _clientService = clientService;
    private readonly IUserService _userService = userService;
    private readonly DaprClient _daprClient = daprClient;
    private readonly DatabaseContext _databaseContext = databaseContext;
    private readonly ITransactionService _transactionService = transactionService;
    private readonly IClaimHandlerService _claimService = claimService;
    private readonly IConsentService _consentService = consentService;
    private IInternetBankingUserService? _internetBankingUserService = internetBankingUserService;
    private IbDatabaseContext _ibContext = ibContext;
    private IbDatabaseContextMordor _ibContextMordor = ibContextMordor;
    private IbSecurityDatabaseContext _ibSecurityContext = securityContext;
    private IProfileService? _profileService = profileService;
    private IRoleService? _roleService = roleService;

    private TokenInfoDetail _tokenInfoDetail = new();
    private GenerateTokenRequest? _tokenRequest;
    private ClientResponse? _client;
    private ConsentResponse? _consent;
    private LoginResponse? _user;
    private SimpleProfileResponse? _profile;
    private ConsentDto? _selectedConsent;
    private RoleDefinitionDto? _role;
    private core.Models.Collection.User? _collectionUser;
    private string? _deviceId;

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

        if (accessTokenInfo != null)
        {
            await _daprClient.SaveStateAsync(Configuration["DAPR_STATE_STORE_NAME"], accessTokenInfo!.Id.ToString(), accessTokenInfo, metadata: new Dictionary<string, string> { { "ttlInSeconds", _tokenInfoDetail.AccessTokenDuration.ToString() } });
        }
        if (refreshTokenInfo != null)
        {
            await _daprClient.SaveStateAsync(Configuration["DAPR_STATE_STORE_NAME"], refreshTokenInfo!.Id.ToString(), refreshTokenInfo, metadata: new Dictionary<string, string> { { "ttlInSeconds", _tokenInfoDetail.RefreshTokenDuration.ToString() } });
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
        if (identityInfo == null)
            return string.Empty;

        if (identityInfo.claims != null && identityInfo.claims.Count() > 0)
        {
            if(_tokenRequest.GrantType.Equals("client_credentials"))
            {
                identityInfo.claims = identityInfo.claims.Where(c => !IsUserBasedClaim(c)).ToList();
            }
            var populatedClaims = await _claimService.PopulateClaims(identityInfo.claims, _user, _profile, collectionUser : _collectionUser);
            if (_client.id.Equals("3fa85f64-5717-4562-b3fc-2c963f66afa6"))
            {
                claims.Add(new Claim("client_id", "3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                claims.Add(new Claim("email", _profile?.data?.emails?.FirstOrDefault(m => m.type.Equals("personal"))?.address ?? ""));
                claims.Add(new Claim("phone_number", _profile?.data?.phones?.FirstOrDefault(p => p.type.Equals("mobile"))?.ToString() ?? ""));
                claims.Add(new Claim("role", "FullAuthorized"));
                claims.Add(new Claim("credentials", "IsInternetCustomer###1"));
                claims.Add(new Claim("credentials", "IsAnonymous###1"));
                claims.Add(new Claim("azp", "3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                claims.Add(new Claim("logon_ip", _transactionService.IpAddress ?? "undefined"));
            }
            claims.AddRange(populatedClaims);

        }
        if(_user is {})
        {
            claims.Add(new Claim("sub", _user.Reference.ToString()));
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
        , new List<string>(), _user!.Id, _tokenInfoDetail.AccessTokenId, null, _deviceId));

        return idToken;
    }

    private async Task<string> CreateAccessToken()
    {
        _tokenInfoDetail.AccessTokenId = Guid.NewGuid();

        var tokenClaims = new List<Claim>();
        var excludedScopes = new string[] { "openId", "profile" };
        foreach (var scope in _tokenRequest!.Scopes!.ToArray().Except(excludedScopes))
            tokenClaims.Add(new Claim("scope", scope));

        if (_tokenRequest.GrantType == "client_credentials")
        {
            tokenClaims.Add(new Claim("scope", "client_credentials"));
        }

        if (_tokenRequest.GrantType == "device")
        {
            tokenClaims.Add(new Claim("scope", "device"));
        }

        var accessInfo = _client!.tokens!.FirstOrDefault(t => t.type == 0);
        if (accessInfo == null)
            return string.Empty;

        int accessDuration = 0;
        try
        {
            accessDuration = TimeHelper.ConvertStrDurationToSeconds(accessInfo.duration!);
            if(_consent is not null)
            {
                if(_consent.consentType!.Equals("OB_Account"))
                {
                    accessDuration = 24 * 60 * 60; // 1 day
                }
                if(_consent.consentType.Equals("OB_Payment"))
                {
                    accessDuration = 5 * 60; // 5 min
                }
            }
            _tokenInfoDetail.AccessTokenDuration = accessDuration;
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex.Message);
            return string.Empty;
        }

        if (accessInfo.claims != null && accessInfo.claims.Count() > 0)
        {
            if(_tokenRequest.GrantType.Equals("client_credentials"))
            {
                accessInfo.claims = accessInfo.claims.Where(c => !IsUserBasedClaim(c)).ToList();
            }
            var populatedClaims = await _claimService.PopulateClaims(accessInfo.claims, _user, _profile, _consent, collectionUser : _collectionUser);
            tokenClaims.AddRange(populatedClaims);
            if (_tokenRequest.Scopes.Contains("temporary"))
                tokenClaims.Add(new Claim("isTemporary", "1"));

            tokenClaims.Add(new Claim("client_id", _client.code ?? _client.id));

            //TODO
            //Set Claims Dynamically
            if (_client.id.Equals("3fa85f64-5717-4562-b3fc-2c963f66afa6"))
            {
                tokenClaims.Add(new Claim("email", _profile?.data?.emails?.FirstOrDefault(m => m.type.Equals("personal"))?.address ?? ""));
                tokenClaims.Add(new Claim("phone_number", _profile?.data?.phones?.FirstOrDefault(p => p.type.Equals("mobile"))?.ToString() ?? ""));
                if(_user?.Reference == "99999999998")
                {
                    tokenClaims.Add(new Claim("role", "Viewer"));
                }
                else
                {
                    try
                    {
                        var roleName = _role.Tags.First();
                        //TODO - Throw Exception For Else Case
                        if(!string.IsNullOrWhiteSpace(roleName))
                            tokenClaims.Add(new Claim("role", roleName));
                        else
                            tokenClaims.Add(new Claim("role", "FullAuthorized"));
                    }
                    catch (Exception)
                    {
                        tokenClaims.Add(new Claim("role", "FullAuthorized"));
                    }
                    
                }
                
                
                tokenClaims.Add(new Claim("credentials", "IsInternetCustomer###1"));
                tokenClaims.Add(new Claim("credentials", "IsAnonymous###1"));
                tokenClaims.Add(new Claim("azp", "3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                tokenClaims.Add(new Claim("uppercase_name", (_profile?.data?.profile?.uppercase_name ?? string.Empty) + (string.IsNullOrWhiteSpace(_profile?.data?.profile?.middleName) ? string.Empty : _profile?.data?.profile?.middleName)));
                tokenClaims.Add(new Claim("uppercase_surname", (_profile?.data?.profile?.uppercase_surname ?? string.Empty)));
                tokenClaims.Add(new Claim("logon_ip", _transactionService.IpAddress ?? "undefined"));
            }

            
        }
        tokenClaims.Add(new Claim("jti", _tokenInfoDetail.AccessTokenId.ToString()));
        
        if (_user != null)
            tokenClaims.Add(new Claim("userId", _user!.Id.ToString()));
        else
            tokenClaims.Add(new Claim("clientAuthorized", "1"));

        tokenClaims.Add(new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));

        if (accessInfo.privateClaims != null && accessInfo.privateClaims.Count() > 0)
        {
            var populatedPrivateClaims = await _claimService.PopulatePrivateClaims(accessInfo.privateClaims, _user, _profile, _consent, collectionUser : _collectionUser);
            var dictFromPrivateClaims = populatedPrivateClaims.Where(c => c is not null).ToDictionary(c => c!.Value.Key, c => c!.Value.Value);
            await _daprClient.SaveStateAsync(Configuration["DAPR_STATE_STORE_NAME"], $"{_tokenInfoDetail!.AccessTokenId.ToString()}_privateClaims", dictFromPrivateClaims, metadata: new Dictionary<string, string> { { "ttlInSeconds", (_tokenInfoDetail.AccessTokenDuration+60).ToString() } });
        }

        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_client.jwtSalt!));
        var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha384);

        var expires = DateTime.UtcNow.AddSeconds(accessDuration);

        string access_token = JwtHelper.GenerateJwt("BurganIam", _client.returnuri, tokenClaims,
            expires: expires, signingCredentials: signinCredentials);

        var scopes = _tokenRequest.Scopes!.ToArray().Except(excludedScopes).ToList();


        _tokenInfoDetail.TokenList.Add(JwtHelper.CreateTokenInfo(TokenType.AccessToken, _tokenInfoDetail.AccessTokenId, _client.id!, DateTime.UtcNow.AddSeconds(accessDuration), true, _user?.Reference ?? ""
        , scopes, _user?.Id ?? null, null, _consent?.id, _deviceId));

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
            if (_user != null)
                tokenClaims.Add(new Claim("userId", _user!.Id.ToString()));
        }
        if (refreshInfo == null)
            return string.Empty;

        int refreshDuration = 0;
        try
        {
            refreshDuration = TimeHelper.ConvertStrDurationToSeconds(refreshInfo.duration!);
            if(_consent is not null)
            {
                if(_consent.consentType.Equals("OB_Account"))
                {
                    refreshDuration = 90 * 24 * 60 * 60; // Until Consent Expires
                }
                if(_consent.consentType.Equals("OB_Payment"))
                {
                    refreshDuration = 15 * 24 * 60 * 60; // 15 day
                }
            }
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

        _tokenInfoDetail.TokenList.Add(JwtHelper.CreateTokenInfo(TokenType.RefreshToken, _tokenInfoDetail.RefreshTokenId, _client.id!, DateTime.UtcNow.AddSeconds(refreshDuration), true, _user?.Reference ?? ""
        , new List<string>(), _user?.Id ?? null, _tokenInfoDetail.AccessTokenId, _tokenRequest!.ConsentId, _deviceId));

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
        if (!_tokenRequest.GrantType.Equals("client_credentials"))
        {
            if (_tokenRequest!.Scopes!.Contains("openId") || _tokenRequest!.Scopes!.Contains("profile"))
            {
                tokenResponse.IdToken = await CreateIdToken();
            }
        }

        return tokenResponse;
    }

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithRefreshToken(GenerateTokenRequest tokenRequest)
    {
        _tokenRequest = tokenRequest;

        var refreshTokenJti = JwtHelper.GetClaim(tokenRequest.RefreshToken!, "jti");
        if (refreshTokenJti == null)
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
        
        //OpenBanking Set Consent
        if(relatedToken.ConsentId is Guid)
        {
            var consent = await _consentService.GetConsent(relatedToken.ConsentId.Value);
            _consent = consent.Response;
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

        if (refreshTokenInfo.UserId != null)
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

            var profile = await _profileService!.GetCustomerSimpleProfile(refreshTokenInfo.Reference!);
            if (profile.StatusCode != 200)
            {
                return new ServiceResponse<TokenResponse>()
                {
                    StatusCode = 500,
                    Detail = "External Api Call Failed | Profile Service"
                };
            }
            _profile = profile.Response;

            if (_user?.State.ToLower() != "active" && _user?.State.ToLower() != "new")
            {
                return new ServiceResponse<TokenResponse>()
                {
                    StatusCode = 470,
                    Detail = "User is disabled"
                };
            }
        }
        else
        {
            _user = null;
            _tokenRequest.GrantType = "client_credentials";
        }

        var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_client.jwtSalt!));
        JwtSecurityToken? refreshTokenValidated;
        if (JwtHelper.ValidateToken(tokenRequest.RefreshToken!, "BurganIam", _client.returnuri, secretKey, out refreshTokenValidated))
        {

            var tokenResponse = await GenerateTokenResponse();

            if (tokenResponse.AccessToken == string.Empty || tokenResponse.RefreshToken == string.Empty)
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

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithPasswordFromWorkflow(GenerateTokenRequest tokenRequest, ClientResponse client, LoginResponse user, SimpleProfileResponse? profile, string deviceId = "")
    {
        _tokenRequest = tokenRequest;

        _client = client;
        _user = user;
        _profile = profile;
        _deviceId = deviceId;

        var tokenResponse = await GenerateTokenResponse();

        if (tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 500,
                Detail = "Access Token And Id Token Couldn't Be Generated"
            };
        }

        await _databaseContext.Tokens.Where(t => t.Reference == _tokenRequest.Username && t.IsActive).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false));
        await PersistTokenInfo();

        return new ServiceResponse<TokenResponse>()
        {
            StatusCode = 200,
            Response = tokenResponse
        };
    }
    
    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithDevice(GenerateTokenRequest tokenRequest)
    {
        _tokenRequest = tokenRequest;
        var checkDeviceResponse = await _userService.CheckDeviceWithoutUser(_tokenRequest.ClientId, _tokenRequest.DeviceId, Guid.Parse(_tokenRequest.InstallationId));
        if (checkDeviceResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = checkDeviceResponse.StatusCode,
                Detail = checkDeviceResponse.Detail
            };
        }
        if (!_tokenRequest.Username.Equals(checkDeviceResponse.Response.Reference))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 490,
                Detail = "User Has No Authorize On Requested Device"
            };
        }

        ServiceResponse<ClientResponse> clientResponse;
        if (Guid.TryParse(_tokenRequest.ClientId!, out Guid _))
        {
            clientResponse = await _clientService.ValidateClient(_tokenRequest.ClientId!, _tokenRequest.ClientSecret!);
        }
        else
        {
            clientResponse = await _clientService.ValidateClientByCode(_tokenRequest.ClientId!, _tokenRequest.ClientSecret!);
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

        var userResponse = await _internetBankingUserService.GetUser(_tokenRequest.Username!);
        if (userResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 404,
                Detail = "User Not Found"
            };
        }
        var user = userResponse.Response;

        var userStatus = await _ibContext.Status.Where(s => s.UserId == user!.Id && (!s.State.HasValue || s.State.Value == 10)).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync();
        if (userStatus?.Type == 30 || userStatus?.Type == 40)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 404,
                Detail = "User Not Active"
            };
        }

        var userInfoResult = await _profileService.GetCustomerSimpleProfile(_tokenRequest.Username!);
        if (userInfoResult.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "User Profile Couldn't Be Fetched"
            };
        }

        var userInfo = userInfoResult.Response;
        _profile = userInfo;
        if (userInfo!.data!.profile!.Equals("customer") || !userInfo!.data!.profile!.status!.Equals("active"))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "User is Not Customer Or Not Active"
            };
        }

        var mobilePhoneCount = userInfo!.data!.phones!.Count(p => p.type!.Equals("mobile"));
        if (mobilePhoneCount != 1)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Bad Phone Data"
            };
        }

        var mobilePhone = userInfo!.data!.phones!.FirstOrDefault(p => p.type!.Equals("mobile"));
        if (string.IsNullOrWhiteSpace(mobilePhone!.prefix) || string.IsNullOrWhiteSpace(mobilePhone!.number))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Bad Phone Format"
            };
        }

        var userRequest = new UserInfo
        {
            firstName = userInfo!.data.profile!.name!,
            lastName = userInfo!.data.profile!.name!,
            phone = new core.Models.User.UserPhone()
            {
                countryCode = mobilePhone!.countryCode!,
                prefix = mobilePhone!.prefix,
                number = mobilePhone!.number
            },
            state = "Active",
            password = _tokenRequest.Password!,
            explanation = "Migrated From IB",
            reason = "Amorphie Login"
        };
        var verifiedMailAddress = userInfo.data.emails!.FirstOrDefault(m => m.isVerified == true);
        userRequest.eMail = verifiedMailAddress?.address ?? "";
        userRequest.reference = _tokenRequest.Username!;

        var migrateResult = await _userService.SaveUser(userRequest);

        var userAmorphieResponse = await _userService.Login(new LoginRequest() { Reference = tokenRequest.Username!, Password = tokenRequest.Password! });

        if (userAmorphieResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = userResponse.StatusCode,
                Detail = userResponse.Detail
            };
        }

        _user = userAmorphieResponse.Response;

        if (_user?.State.ToLower() != "active" && _user?.State.ToLower() != "new")
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 470,
                Detail = "User is disabled"
            };
        }

        var tokenResponse = await GenerateTokenResponse();

        if (tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
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
        ServiceResponse<ClientResponse> clientResponse;
        if (Guid.TryParse(_tokenRequest.ClientId!, out Guid _))
        {
            clientResponse = await _clientService.ValidateClient(_tokenRequest.ClientId!, _tokenRequest.ClientSecret!);
        }
        else
        {
            clientResponse = await _clientService.ValidateClientByCode(_tokenRequest.ClientId!, _tokenRequest.ClientSecret!);
        }

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
    
        var userResponse = await _internetBankingUserService.GetUser(_tokenRequest.Username!);
        if (userResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 404,
                Detail = "User Not Found"
            };
        }
        var user = userResponse.Response;

        var userStatus = await _ibContext.Status.Where(s => s.UserId == user!.Id && (!s.State.HasValue || s.State.Value == 10)).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync();

        var passwordResponse = await _internetBankingUserService.GetPassword(user!.Id);
        if (userResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 404,
                Detail = "User Not Found"
            };
        }
        var passwordRecord = passwordResponse.Response;

        var isVerified = _internetBankingUserService.VerifyPassword(passwordRecord!.HashedPassword!, _tokenRequest.Password!, passwordRecord.Id.ToString());
        //Consider SuccessRehashNeeded
        if (isVerified != PasswordVerificationResult.Success)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Password Doesn't Match"
            };
        }

        var mordorUserResponse = await internetBankingUserService.MordorGetUser(_tokenRequest.Username!);
        if (mordorUserResponse.StatusCode == 200)
        {
            var mordorUser = mordorUserResponse.Response;

            var role = await ibContextMordor.Role.Where(r => r.UserId.Equals(mordorUser!.Id) && r.Channel.Equals(10) && r.Status.Equals(10)).OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync();
            if(role is {} && (role.ExpireDate ?? DateTime.MinValue) > DateTime.Now)
            {
                var roleDefinition = await ibContextMordor.RoleDefinition.FirstOrDefaultAsync(d => d.Id.Equals(role.DefinitionId) && d.IsActive && d.Key.Equals(0));
                if(roleDefinition is {})
                {
                    return new ServiceResponse<TokenResponse>()
                    {
                        StatusCode = 471,
                        Detail = "User is Not Active"
                    };
                }
            }
        }

        var userInfoResult = await _profileService.GetCustomerSimpleProfile(_tokenRequest.Username!);
        if (userInfoResult.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "User Profile Couldn't Be Fetched"
            };
        }

        var userInfo = userInfoResult.Response;
        _profile = userInfo;
        if (userInfo!.data!.profile!.Equals("customer") || !userInfo!.data!.profile!.status!.Equals("active"))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "User is Not Customer Or Not Active"
            };
        }

        var mobilePhoneCount = userInfo!.data!.phones!.Count(p => p.type!.Equals("mobile"));
        if (mobilePhoneCount != 1)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Bad Phone Data"
            };
        }

        var mobilePhone = userInfo!.data!.phones!.FirstOrDefault(p => p.type!.Equals("mobile"));
        if (string.IsNullOrWhiteSpace(mobilePhone!.prefix) || string.IsNullOrWhiteSpace(mobilePhone!.number))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Bad Phone Format"
            };
        }

        var userRequest = new UserInfo
        {
            firstName = userInfo!.data.profile!.name!,
            lastName = userInfo!.data.profile!.name!,
            phone = new core.Models.User.UserPhone()
            {
                countryCode = mobilePhone!.countryCode!,
                prefix = mobilePhone!.prefix,
                number = mobilePhone!.number
            },
            state = "Active",
            salt = passwordRecord.Id.ToString(),
            password = _tokenRequest.Password!,
            explanation = "Migrated From IB",
            reason = "Amorphie Login",
            isArgonHash = true
        };
        var verifiedMailAddress = userInfo.data.emails!.FirstOrDefault(m => m.isVerified == true);
        userRequest.eMail = verifiedMailAddress?.address ?? "";
        userRequest.reference = _tokenRequest.Username!;

        var migrateResult = await _userService.SaveUser(userRequest);

        var userAmorphieResponse = await _userService.Login(new LoginRequest() { Reference = tokenRequest.Username!, Password = tokenRequest.Password! });

        if (userAmorphieResponse.StatusCode != 200)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = userResponse.StatusCode,
                Detail = userResponse.Detail
            };
        }

        _user = userAmorphieResponse.Response;

        if (_user?.State.ToLower() != "active" && _user?.State.ToLower() != "new")
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 470,
                Detail = "User is disabled"
            };
        }

        // var consentListResponse = await _roleService.GetConsents(_client.code, _user.Reference);
        // if(consentListResponse.StatusCode != 200)
        // {
        //     return new ServiceResponse<TokenResponse>()
        //     {
        //         StatusCode = consentListResponse.StatusCode,
        //         Detail = consentListResponse.Detail
        //     };
        // }
        // var consentList = consentListResponse.Response;

        // if(consentList.Count() != 1)
        // {
        //     return new ServiceResponse<TokenResponse>()
        //     {
        //         StatusCode = 480,
        //         Detail = "Password Grant Type Doesn't Support Multiple Consents"
        //     };
        // }

        // var consent = consentList.First();
        // _selectedConsent = consent;

        // var roleResponse = await _roleService.GetRoleDefinition(consent.RoleId);
        // if(roleResponse.StatusCode != 200)
        // {
        //     return new ServiceResponse<TokenResponse>()
        //     {
        //         StatusCode = roleResponse.StatusCode,
        //         Detail = roleResponse.Detail
        //     };
        // }
        // var role = roleResponse.Response;
        // _role = role;

        var tokenResponse = await GenerateTokenResponse();

        if (tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
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

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithSystemUser(GenerateTokenRequest tokenRequest)
    {
        _tokenRequest = tokenRequest;
        ServiceResponse<ClientResponse> clientResponse;
        if (Guid.TryParse(_tokenRequest.ClientId!, out Guid _))
        {
            clientResponse = await _clientService.ValidateClient(_tokenRequest.ClientId!, _tokenRequest.ClientSecret!);
        }
        else
        {
            clientResponse = await _clientService.ValidateClientByCode(_tokenRequest.ClientId!, _tokenRequest.ClientSecret!);
        }

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

        var user = await _ibSecurityContext.AspNetUsers.AsNoTracking().FirstOrDefaultAsync(u => u.UserName.Equals(_tokenRequest.Username));
        if(user is not {})
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 404,
                Detail = "User Not Found"
            };
        }

        var passwordHasher = new PasswordHasher();
        var isVerified = passwordHasher.VerifyHashedPassword(user!.PasswordHash!, _tokenRequest.Password!);
        //Consider SuccessRehashNeeded
        if (isVerified != PasswordVerificationResult.Success)
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 471,
                Detail = "Password Doesn't Match"
            };
        }

        var tokenResponse = await GenerateTokenResponse();

        if (tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
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

    public async Task<ServiceResponse<TokenResponse>> GenerateTokenWithClientCredentials(GenerateTokenRequest tokenRequest)
    {
        _tokenRequest = tokenRequest;
        _user = null;
        ServiceResponse<ClientResponse> clientResponse;
        if (Guid.TryParse(_tokenRequest.ClientId!, out Guid _))
        {
            clientResponse = await _clientService.ValidateClient(_tokenRequest.ClientId!, _tokenRequest.ClientSecret!);
        }
        else
        {
            clientResponse = await _clientService.ValidateClientByCode(_tokenRequest.ClientId!, _tokenRequest.ClientSecret!);
        }

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

        var tokenResponse = await GenerateTokenResponse();

        if (tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
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
        ServiceResponse<ClientResponse> clientResponse;

        if(Guid.TryParse(tokenRequest.ClientId, out Guid _))
        {
            clientResponse = await _clientService.ValidateClient(tokenRequest.ClientId!, tokenRequest.ClientSecret!);
        }
        else
        {
            clientResponse = await _clientService.ValidateClientByCode(tokenRequest.ClientId!, tokenRequest.ClientSecret!);
        }
        
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
        _profile = authorizationCodeInfo.Profile;
        _collectionUser = authorizationCodeInfo.CollectionUser;
        _tokenRequest.Scopes = authorizationCodeInfo.RequestedScopes;

        if (!authorizationCodeInfo.ClientId!.Equals(tokenRequest.ClientId, StringComparison.OrdinalIgnoreCase))
        {
            return new ServiceResponse<TokenResponse>()
            {
                StatusCode = 472,
                Detail = "ClientId Not Matched"
            };
        }

        if (_client.pkce == "must")
        {
            if (!authorizationCodeInfo.CodeChallenge!.Equals(GetHashedCodeVerifier(tokenRequest.CodeVerifier!)))
            {
                return new ServiceResponse<TokenResponse>()
                {
                    StatusCode = 473,
                    Detail = "Code Verifier Not Matched"
                };
            }
        }

        var tokenResponse = await GenerateTokenResponse();

        if (tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
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

    public async Task<ServiceResponse<TokenResponse>> GenerateOpenBankingToken(GenerateTokenRequest tokenRequest, ConsentResponse consent)
    {
        _tokenRequest = tokenRequest;
        _consent = consent;

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
        tokenResponse.scope = string.Join(" ",authorizationCodeInfo!.RequestedScopes!);

        if (tokenResponse.IdToken == string.Empty && tokenResponse.AccessToken == string.Empty)
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

    private List<string> GetClientClaims(List<string> clientClaims)
    {
        return clientClaims.Where(c => !IsUserBasedClaim(c)).ToList();
    }

    private string GetClaimSourcePath(string claim)
    {
        var claimInfoList = claim.Split("|");
        if(claimInfoList.Count() > 1)
        {
            return claimInfoList[1];
        }
        else
        {
            return claimInfoList[0];
        }
    }

    private bool IsUserBasedClaim(string claim)
    {
        var claimSource = GetClaimSourcePath(claim);
        return GetClaimInfoSource(claimSource) switch 
            {
                "const" => false,
                _ => true
            };
    }

    private string GetClaimInfoSource(string claimInfoPath)
    {
        var claimInfoPathList = claimInfoPath.Split(".");
        return claimInfoPathList[0];
    }
}
