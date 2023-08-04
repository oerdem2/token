
namespace amorphie.token.Services.User;

public interface IUserService
{
    public Task<ServiceResponse<LoginResponse>> Login(LoginRequest loginRequest);
}
