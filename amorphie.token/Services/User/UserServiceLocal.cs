
using System.Text;
using System.Text.Json;
using MongoDB.Bson;

namespace amorphie.token.Services.User;

public class UserServiceLocal : IUserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    public UserServiceLocal(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ServiceResponse<object>> CheckDevice(Guid userId, string clientId, string deviceId, Guid installationId)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var httpResponseMessage = await httpClient.GetAsync($"userDevice/check-device/{clientId}/{userId}/{deviceId}/{installationId}");

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceResponse<object>() { StatusCode = 200, Response = null };
        }
        else
        {
            return new ServiceResponse<object>() { StatusCode = 404, Response = "Device Not Found" };
        }
    }

    public async Task<ServiceResponse<CheckDeviceWithoutUserResponseDto>> CheckDeviceWithoutUser(string clientId, string deviceId, Guid installationId)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var httpResponseMessage = await httpClient.GetAsync($"userDevice/check-device-without-user/{clientId}/{deviceId}/{installationId}");

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            var response = await httpResponseMessage.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<CheckDeviceWithoutUserResponseDto>(response);
            return new ServiceResponse<CheckDeviceWithoutUserResponseDto>() { StatusCode = 200, Response = responseObject };
        }
        else
        {
            return new ServiceResponse<CheckDeviceWithoutUserResponseDto>() { StatusCode = 404, Response = null };
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
            "user/" + userId.ToString());

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

    public async Task<ServiceResponse> SaveUser(UserInfo userInfo)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var request = new StringContent(JsonSerializer.Serialize(userInfo), Encoding.UTF8, "application/json");
        var httpResponseMessage = await httpClient.PostAsync(
            "user", request);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceResponse() { StatusCode = 200 };
        }
        else
        {
            throw new ServiceException((int)Errors.InvalidUser, "User Endpoint Did Not Response Successfully");
        }
    }

    public async Task<ServiceResponse> SaveDevice(UserSaveMobileDeviceDto userSaveMobileDeviceDto)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var request = new StringContent(JsonSerializer.Serialize(userSaveMobileDeviceDto), Encoding.UTF8, "application/json");
        var httpResponseMessage = await httpClient.PostAsync(
            "userDevice/save-mobile-device-client", request);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceResponse() { StatusCode = 200 };
        }
        else
        {
            throw new ServiceException((int)Errors.InvalidUser, "User Endpoint Did Not Response Successfully");
        }
    }

    public async Task<ServiceResponse> RemoveDevice(string reference, string clientId)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var httpResponseMessage = await httpClient.PutAsync(
            "public/device/remove/" + clientId + "/" + reference, null);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceResponse() { StatusCode = 200 };
        }
        else
        {
            throw new ServiceException((int)Errors.InvalidUser, "User Endpoint Did Not Response Successfully");
        }
    }

    public async Task<ServiceResponse<LoginResponse>> GetUserByReference(string reference)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var httpResponseMessage = await httpClient.GetAsync(
            "user/reference/" + reference.ToString());

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

    public async Task<ServiceResponse> MigrateSecurityQuestion(MigrateSecurityQuestionRequest migrateSecurityQuestionRequest)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var request = new StringContent(JsonSerializer.Serialize(migrateSecurityQuestionRequest), Encoding.UTF8, "application/json");
        var httpResponseMessage = await httpClient.PostAsync(
            "userSecurityQuestion/migrate", request);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceResponse() { StatusCode = 200 };
        }
        else
        {
            return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode, Detail = "Migrate Security Question Error" };
        }
    }

    public async Task<ServiceResponse> MigrateSecurityImage(MigrateSecurityImageRequest migrateSecurityImageRequest)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var request = new StringContent(JsonSerializer.Serialize(migrateSecurityImageRequest), Encoding.UTF8, "application/json");
        var httpResponseMessage = await httpClient.PostAsync(
            "userSecurityImage/migrate", request);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceResponse() { StatusCode = 200 };
        }
        else
        {
            return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode, Detail = "Migrate Security Image Error" };
        }
    }

    public async Task<ServiceResponse> MigrateSecurityQuestions(List<SecurityQuestionRequestDto> securityQuestionRequestDtos)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var request = new StringContent(JsonSerializer.Serialize(securityQuestionRequestDtos), Encoding.UTF8, "application/json");
        var httpResponseMessage = await httpClient.PostAsync(
            "userSecurityQuestion/migrateQuestions", request);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceResponse() { StatusCode = 200 };
        }
        else
        {
            return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode, Detail = "Migrate Security Questions Error" };
        }
    }

    public async Task<ServiceResponse> MigrateSecurityImages(List<SecurityImageRequestDto> securityImageRequestDtos)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var request = new StringContent(JsonSerializer.Serialize(securityImageRequestDtos), Encoding.UTF8, "application/json");
        var httpResponseMessage = await httpClient.PostAsync(
            "userSecurityImage/migrateImages", request);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceResponse() { StatusCode = 200 };
        }
        else
        {
            return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode, Detail = "Migrate Security Images Error" };
        }
    }
}
