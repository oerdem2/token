

namespace amorphie.token.Services.User;

public class UserService : ServiceBase, IUserService
{
    private readonly DaprClient _daprClient;
    public UserService(DaprClient daprClient, ILogger<UserService> logger, IConfiguration configuration) : base(logger, configuration)
    {
        _daprClient = daprClient;
    }

    public async Task<ServiceResponse<object>> CheckDevice(Guid userId, Guid clientId)
    {
        try
        {
            await _daprClient.InvokeMethodAsync(HttpMethod.Get, Configuration["UserServiceAppName"], $"/userDevice/search?Page=0&PageSize=50&Keyword={userId}&&{clientId}");
            Console.WriteLine("Device bulundu");
            return new ServiceResponse<object>()
            {
                StatusCode = 200,
                Detail = "",
                Response = null
            };
        }
        catch (InvocationException ex)
        {

            if ((int)ex.Response.StatusCode >= 400 && (int)ex.Response.StatusCode < 500)
            {
                if ((int)ex.Response.StatusCode == 404)
                {
                    return new ServiceResponse<object>()
                    {
                        StatusCode = 404,
                        Detail = "User Device Not Found"
                    };
                }
                return new ServiceResponse<object>()
                {
                    StatusCode = (int)ex.Response.StatusCode,
                    Detail = "User Device Not Found"
                };
            }
            else
            {
                Logger.LogError("An Error Occured At User Device Invocation | Detail:" + ex.ToString());
                return new ServiceResponse<object>()
                {
                    StatusCode = 500,
                    Detail = "Server Error"
                };
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError("An Error Occured At User Device Invocation | Detail:" + ex.ToString());
        }
        return null;
    }

    public async Task<ServiceResponse<LoginResponse>> Login(LoginRequest loginRequest)
    {
        try
        {
            var user = await _daprClient.InvokeMethodAsync<LoginRequest, LoginResponse>(Configuration["UserServiceAppName"], "/user/login",
            new()
            {
                Reference = loginRequest.Reference,
                Password = loginRequest.Password
            });
            if (user == null)
            {
                return new ServiceResponse<LoginResponse>()
                {
                    StatusCode = 460,
                    Detail = "User Not Found"
                };
            }
            return new ServiceResponse<LoginResponse>()
            {
                StatusCode = 200,
                Detail = "",
                Response = user
            };
        }
        catch (InvocationException ex)
        {

            if ((int)ex.Response.StatusCode >= 400 && (int)ex.Response.StatusCode < 500)
            {
                if ((int)ex.Response.StatusCode == 460)
                {
                    return new ServiceResponse<LoginResponse>()
                    {
                        StatusCode = 460,
                        Detail = "User Not Found"
                    };
                }
                if ((int)ex.Response.StatusCode == 461)
                {
                    return new ServiceResponse<LoginResponse>()
                    {
                        StatusCode = 461,
                        Detail = "Invalid Reference or Password"
                    };
                }
            }
            else
            {
                Logger.LogError("An Error Occured At User Invocation | Detail:" + ex.ToString());
                return new ServiceResponse<LoginResponse>()
                {
                    StatusCode = 500,
                    Detail = "Server Error"
                };
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError("An Error Occured At User Invocation | Detail:" + ex.ToString());
        }
        return null;
    }

    public async Task<ServiceResponse<LoginResponse>> GetUserById(Guid userId)
    {
        try
        {
            var user = await _daprClient.InvokeMethodAsync<LoginResponse>(Configuration["UserServiceAppName"], "/user/"+userId);
            if (user == null)
            {
                return new ServiceResponse<LoginResponse>()
                {
                    StatusCode = 460,
                    Detail = "User Not Found"
                };
            }
            return new ServiceResponse<LoginResponse>()
            {
                StatusCode = 200,
                Detail = "",
                Response = user
            };
        }
        catch (InvocationException ex)
        {

            if ((int)ex.Response.StatusCode >= 400 && (int)ex.Response.StatusCode < 500)
            {
                if ((int)ex.Response.StatusCode == 460)
                {
                    return new ServiceResponse<LoginResponse>()
                    {
                        StatusCode = 460,
                        Detail = "User Not Found"
                    };
                }
                if ((int)ex.Response.StatusCode == 461)
                {
                    return new ServiceResponse<LoginResponse>()
                    {
                        StatusCode = 461,
                        Detail = "Invalid Reference or Password"
                    };
                }
            }
            else
            {
                Logger.LogError("An Error Occured At User Invocation | Detail:" + ex.ToString());
                return new ServiceResponse<LoginResponse>()
                {
                    StatusCode = 500,
                    Detail = "Server Error"
                };
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError("An Error Occured At User Invocation | Detail:" + ex.ToString());
        }
        return null;
    }
}
