

using amorphie.token.core.Models.Profile;

namespace amorphie.token.Services.Authorization;

public interface IAuthorizationService
{
    public Task<ServiceResponse<AuthorizationResponse>> Authorize(AuthorizationServiceRequest request);
    public Task<AuthorizationCode> AssignUserToAuthorizationCode(LoginResponse user, string authorizationCode,SimpleProfileResponse profile);
}
