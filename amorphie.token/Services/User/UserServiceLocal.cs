
namespace amorphie.token.Services.User;

public class UserServiceLocal : IUserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    public UserServiceLocal(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ServiceResponse<object>> CheckDevice(Guid userId, Guid clientId)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var httpResponseMessage = await httpClient.GetAsync("user/device");

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceResponse<object>() { StatusCode = 200, Response = null };
        }
        else
        {
            throw new ServiceException(404, "Device Not Found");
        }
    }

    public async Task<ServiceResponse<LoginResponse>> Login(LoginRequest loginRequest)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var httpResponseMessage = await httpClient.PostAsJsonAsync<LoginRequest>(
            "user/login", loginRequest);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            var user = await httpResponseMessage.Content.ReadFromJsonAsync<LoginResponse>();
            if (user == null)
            {
                throw new ServiceException((int)Errors.InvalidUser, "User not found with provided info");
            }
            return new ServiceResponse<LoginResponse>() { StatusCode = 200, Response = user };
        }
        else
        {
            throw new ServiceException((int)Errors.InvalidUser, "User Endpoint Did Not Response Successfully");
        }
    }

    public async Task<ServiceResponse<LoginResponse>> GetUserById(Guid userId)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var httpResponseMessage = await httpClient.GetAsync(
            "user/"+userId.ToString());

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            var user = await httpResponseMessage.Content.ReadFromJsonAsync<LoginResponse>();
            if (user == null)
            {
                throw new ServiceException((int)Errors.InvalidUser, "User not found with provided info");
            }
            return new ServiceResponse<LoginResponse>() { StatusCode = 200, Response = user };
        }
        else
        {
            throw new ServiceException((int)Errors.InvalidUser, "User Endpoint Did Not Response Successfully");
        }
    }
}
