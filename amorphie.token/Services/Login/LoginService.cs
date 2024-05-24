using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Migration;

namespace amorphie.token.Services.Login
{
    public class LoginService : ServiceBase, ILoginService
    {
        private readonly IInternetBankingUserService _internetBankingUserService;
        private readonly IUserService _userService;
        private readonly IMigrationService _migrationService;
        public LoginService(
            IInternetBankingUserService internetBankingUserService, IUserService userService, IMigrationService migrationService,
            ILogger<LoginService> logger, IConfiguration configuration):base(logger, configuration)
        {
            _internetBankingUserService = internetBankingUserService;
            _userService = userService;
            _migrationService = migrationService;
        }

        public async Task<ServiceResponse<LoginResponse>> GetAmorphieUser(string username)
        {
            var response = new ServiceResponse<LoginResponse>();
            try
            {
                var amorphieUserResult = await _userService.GetUserByReference(username);
                if(amorphieUserResult.StatusCode != 200)
                {
                    response.StatusCode = amorphieUserResult.StatusCode;
                    response.Detail = amorphieUserResult.Detail;
                    return response;
                }

                response.StatusCode = 200;
                response.Response = amorphieUserResult.Response;
                return response;

            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
                return response;
            }
        }

        public async Task<ServiceResponse> MigrateDodgeUserToAmorphie(string username)
        {
            var response = new ServiceResponse();
            try
            {
                var dodgeUserResult = await _internetBankingUserService.GetUser(username);
                if(dodgeUserResult.StatusCode != 200)
                {
                    response.Detail = dodgeUserResult.Detail;
                    response.StatusCode = dodgeUserResult.StatusCode;
                    return response;
                }
                var dodgeUser = dodgeUserResult.Response;

                var getAmorphieUserFromDodgeResult = await _internetBankingUserService.GetAmorphieUserFromDodge(username);
                if(getAmorphieUserFromDodgeResult.StatusCode != 200)
                {
                    response.Detail = getAmorphieUserFromDodgeResult.Detail;
                    response.StatusCode = getAmorphieUserFromDodgeResult.StatusCode;
                    return response;
                }
                var getAmorphieUserFromDodge = getAmorphieUserFromDodgeResult.Response;

                var saveAmorphieUserResult = await _userService.SaveUser(getAmorphieUserFromDodge!);
                if(saveAmorphieUserResult.StatusCode != 200)
                {
                    response.StatusCode = saveAmorphieUserResult.StatusCode;
                    response.Detail = saveAmorphieUserResult.Detail;
                    return response;
                }

                var amorphieUserResult = await _userService.GetUserByReference(username);
                if(amorphieUserResult.StatusCode != 200)
                {
                    response.StatusCode = amorphieUserResult.StatusCode;
                    response.Detail = amorphieUserResult.Detail;
                    return response;
                }
                var amorphieUser = amorphieUserResult.Response;

                var migrateUserInfoResult = await _migrationService.MigrateUserData(amorphieUser!.Id,dodgeUser.Id);
                if(migrateUserInfoResult.StatusCode != 200)
                {
                    response.StatusCode = migrateUserInfoResult.StatusCode;
                    response.Detail = migrateUserInfoResult.Detail;
                    return response;
                }

                response.StatusCode = 200;
                return response;
                
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
                return response;
            }
        }
    }
}