using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Enums;
using AuthServer.Exceptions;
using AuthServer.Models.User;

namespace AuthServer.Services.User;

public class UserServiceLocal : IUserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    public UserServiceLocal(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<LoginResponse> Login(LoginRequest loginRequest)
    {
        var httpClient = _httpClientFactory.CreateClient("User");
        var httpResponseMessage = await httpClient.PostAsJsonAsync<LoginRequest>(
            "user/login",loginRequest);

        if(httpResponseMessage.IsSuccessStatusCode)
        {
            var user = await httpResponseMessage.Content.ReadFromJsonAsync<LoginResponse>();
            if(user == null)
            {
                throw new ServiceException((int)Errors.InvalidUser,"User not found with provided info");
            }         
            return user;   
        }
        else
        {
            throw new ServiceException((int)Errors.InvalidUser,"User Endpoint Did Not Response Successfully");
        }
    }
}
