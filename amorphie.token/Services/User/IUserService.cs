
namespace amorphie.token.Services.User;

public interface IUserService
{
    public Task<ServiceResponse<LoginResponse>> Login(LoginRequest loginRequest);
    public Task<ServiceResponse<object>> CheckDevice(Guid userId, string clientId, string deviceId, Guid installationId);
    public Task<ServiceResponse<CheckDeviceWithoutUserResponseDto>> CheckDeviceWithoutUser(string clientId, string deviceId, Guid installationId);
    public Task<ServiceResponse> RemoveDevice(string reference, string clientId);
    public Task<ServiceResponse> SaveDevice(UserSaveMobileDeviceDto userSaveMobileDeviceDto);
    public Task<ServiceResponse<LoginResponse>> GetUserById(Guid userId);
    public Task<ServiceResponse<LoginResponse>> GetUserByReference(string reference);
    public Task<ServiceResponse> SaveUser(UserInfo userInfo);
}
