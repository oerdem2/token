using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Enums;
using AuthServer.Exceptions;
using AuthServer.Models.User;
using Dapr.Client;
using token.Models;

namespace AuthServer.Services.User;

public class UserService : ServiceBase, IUserService
{
    private readonly DaprClient _daprClient;
    public UserService(DaprClient daprClient,ILogger<UserService> logger,IConfiguration configuration):base(logger,configuration)
    {
        _daprClient = daprClient;
    }

    public async Task<ServiceResponse<LoginResponse>> Login(LoginRequest loginRequest)
    {
        try
        {
            var user = await _daprClient.InvokeMethodAsync<LoginRequest,LoginResponse>(Configuration["UserServiceAppName"],"/user/login",
            new()
            {
                Reference = loginRequest.Reference,
                Password = loginRequest.Password
            });
            if(user == null)
            {
                return new ServiceResponse<LoginResponse>(){
                    StatusCode = 460,
                    Detail = "User Not Found"
                };
            }         
            return new ServiceResponse<LoginResponse>(){
                    StatusCode = 200,
                    Detail = "",
                    Response = user
                };
        }
        catch(InvocationException ex)
        {
            Logger.LogError("Dapr Service Invocation Failed | Detail:"+ex.Message);

            if((int)ex.Response.StatusCode >= 400 && (int)ex.Response.StatusCode < 500)
            {
                if((int)ex.Response.StatusCode == 460)
                {
                    return new ServiceResponse<LoginResponse>(){
                        StatusCode = 460,
                        Detail = "User Not Found"
                    };
                }
                if((int)ex.Response.StatusCode == 461)
                {
                    return new ServiceResponse<LoginResponse>(){
                        StatusCode = 461,
                        Detail = "Invalid Reference or Password"
                    };
                }
            }
            else
            {
                return new ServiceResponse<LoginResponse>(){
                        StatusCode = 500,
                        Detail = "Server Error"
                    };
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError("An Error Occured At User Invocation | Detail:"+ex.Message);
        }
        return null;
    }
}
