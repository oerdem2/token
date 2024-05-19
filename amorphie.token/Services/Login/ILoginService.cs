using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.Services.Login
{
    public interface ILoginService
    {
        public Task<ServiceResponse> MigrateDodgeUserToAmorphie(string username);
        public Task<ServiceResponse<LoginResponse>> GetAmorphieUser(string username);
    }
}