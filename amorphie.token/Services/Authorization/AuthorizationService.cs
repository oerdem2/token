
using System.Security.Cryptography;
using amorphie.token.core.Extensions;
using Microsoft.IdentityModel.Tokens;
using amorphie.token.data;
using amorphie.token.Services.Profile;
using amorphie.token.Services.Consent;

namespace amorphie.token.Services.Authorization;

public class AuthorizationService : ServiceBase, IAuthorizationService
{
    private readonly IClientService _clientService;
    private readonly DaprClient _daprClient;


    public AuthorizationService(ILogger<AuthorizationService> logger, IConfiguration configuration, IClientService clientService, ITagService tagService,
    IUserService userService, IConsentService consentService, DaprClient daprClient, DatabaseContext databaseContext, IProfileService profileService)
    : base(logger, configuration)
    {
        _clientService = clientService;
        _daprClient = daprClient;
    }

    public async Task AssignUserToAuthorizationCode(LoginResponse user, string authorizationCode)
    {
        var authorizationCodeInfo = await _daprClient.GetStateAsync<AuthorizationCode>(Configuration["DAPR_STATE_STORE_NAME"], authorizationCode);

        var newAuthorizationCodeInfo = authorizationCodeInfo.MapTo<AuthorizationCode>();
        newAuthorizationCodeInfo.Subject = user;

        await _daprClient.SaveStateAsync<AuthorizationCode>(Configuration["DAPR_STATE_STORE_NAME"], authorizationCode, newAuthorizationCodeInfo);
    }

    public async Task<ServiceResponse<AuthorizationResponse>> Authorize(AuthorizationServiceRequest request)
    {
        AuthorizationResponse authorizationResponse = new();
        try
        {
            var clientResponse = await _clientService.CheckClient(request.ClientId!);
            if (clientResponse.StatusCode != 200)
            {
                return new ServiceResponse<AuthorizationResponse>()
                {
                    StatusCode = clientResponse.StatusCode,
                    Detail = clientResponse.Detail
                };
            }
            var client = clientResponse.Response;

            if (string.IsNullOrEmpty(request.ResponseType) || request.ResponseType != "code")
            {
                return new ServiceResponse<AuthorizationResponse>()
                {
                    StatusCode = 473,
                    Detail = "Response Type Parameter Has To Be Set As 'Code'"
                };
            }

            var requestedScopes = request.Scope!.ToList();

            if (!requestedScopes.All(client!.allowedscopetags!.Contains))
            {
                return new ServiceResponse<AuthorizationResponse>()
                {
                    StatusCode = 473,
                    Detail = "Client is Not Authorized For Requested Scopes"
                };
            }

            var authCode = new AuthorizationCode
            {
                ClientId = request.ClientId,
                RedirectUri = request.RedirectUri,
                RequestedScopes = requestedScopes,
                CodeChallenge = request.CodeChallange,
                CodeChallengeMethod = request.CodeChallangeMethod,
                CreationTime = DateTime.UtcNow,
                Subject = request.User ?? null,
                ConsentId = request.ConsentId?.ToString(),
                Nonce = request.Nonce
            };

            var code = await GenerateAuthorizationCode(authCode);

            authorizationResponse.RedirectUri = $"{client.returnuri}?response_type=code&state={request.State}";
            authorizationResponse.Code = code;
            authorizationResponse.RequestedScopes = requestedScopes;
            authorizationResponse.State = request.State!;

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

}
