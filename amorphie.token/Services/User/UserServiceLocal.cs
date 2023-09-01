
namespace amorphie.token.Services.User;

public class UserServiceLocal : IUserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    public UserServiceLocal(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
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
}
