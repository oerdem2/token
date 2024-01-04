

namespace amorphie.token.Services.Authorization;

public interface IAuthorizationService
{
    public Task<ServiceResponse<AuthorizationResponse>> Authorize(AuthorizationServiceRequest request);
    public Task AssignUserToAuthorizationCode(LoginResponse user, string authorizationCode);
}
