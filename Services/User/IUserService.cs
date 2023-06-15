using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Models.User;

namespace AuthServer.Services.User;

public interface IUserService
{
    public Task<LoginResponse> Login(LoginRequest loginRequest);
}
