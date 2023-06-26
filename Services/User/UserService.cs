using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Enums;
using AuthServer.Exceptions;
using AuthServer.Models.User;
using Dapr.Client;

namespace AuthServer.Services.User;

public class UserService : ServiceBase, IUserService
{
    private readonly DaprClient _daprClient;
    public UserService(DaprClient daprClient,ILogger<UserService> logger,IConfiguration configuration):base(logger,configuration)
    {
        _daprClient = daprClient;
    }

    public async Task<LoginResponse> Login(LoginRequest loginRequest)
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
                throw new ServiceException((int)Errors.InvalidUser,"User not found with provided info");
            }         
            return user;   
        }
        catch(InvocationException ex)
        {
            Logger.LogError("Dapr Service Invocation Failed | Detail:"+ex.Message);
            if(ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new ServiceException((int)Errors.InvalidUser,"User not found with provided info");
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError("An Error Occured At Client Invocation | Detail:"+ex.Message);
        }
        return null;
    }
}
