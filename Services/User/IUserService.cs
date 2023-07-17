using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Models.User;
using token.Models;

namespace AuthServer.Services.User;

public interface IUserService
{
    public Task<ServiceResponse<LoginResponse>> Login(LoginRequest loginRequest);
}
