

namespace amorphie.token.Services.Authorization;

public interface IAuthorizationService
{
    public Task<ServiceResponse<AuthorizationResponse>> Authorize(AuthorizationRequest request);
    public Task AssignUserToAuthorizationCode(LoginResponse user, string authorizationCode);
    public Task<ServiceResponse<TokenResponse>> GenerateToken(TokenRequest tokenRequest);
    public Task<ServiceResponse<TokenResponse>> GenerateTokenWithPassword(TokenRequest tokenRequest);
    public Task<ServiceResponse<TokenResponse>> GenerateTokenWithPasswordFromWorkflow(TokenRequest tokenRequest, ClientResponse client, LoginResponse user);
}
