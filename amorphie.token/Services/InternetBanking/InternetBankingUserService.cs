using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using amorphie.token.Services.Profile;
using amorphie.token.Services.TransactionHandler;
using Elastic.CommonSchema;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Services.InternetBanking
{
    
    public class InternetBankingUserService : ServiceBase, IInternetBankingUserService
    {
        private readonly IbDatabaseContext _ibDatabaseContext;
        private readonly IbDatabaseContextMordor _ibMordorDatabaseContext;
        private readonly IProfileService _profileService;
        private readonly ITransactionService _transactionService;
        public InternetBankingUserService(IbDatabaseContext ibDatabaseContext, IProfileService profileService,
        ITransactionService transactionService, IbDatabaseContextMordor ibMordorDatabaseContext, ILogger<InternetBankingUserService> logger, IConfiguration configuration) : base(logger, configuration)
        {
            _ibDatabaseContext = ibDatabaseContext;
            _ibMordorDatabaseContext = ibMordorDatabaseContext;
            _profileService = profileService;
            _transactionService = transactionService;
        }

        public async Task<ServiceResponse<IBPassword>> GetPassword(Guid userId)
        {
            ServiceResponse<IBPassword> response = new();
            try
            {
                var password = await _ibDatabaseContext.Password.Where(p => p.UserId == userId).OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync();
                if (password == null)
                {
                    response.StatusCode = 404;
                    response.Detail = "User Not Found";
                }
                else
                {
                    response.StatusCode = 200;
                    response.Detail = "";
                    response.Response = password;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }

            return response;
        }

        public async Task<ServiceResponse<IBUser>> GetUser(string username)
        {
            ServiceResponse<IBUser> response = new();
            try
            {
                var user = await _ibDatabaseContext.User.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == username);
                if (user == null)
                {
                    response.StatusCode = 404;
                    response.Detail = "User Not Found";
                }
                else
                {
                    response.StatusCode = 200;
                    response.Detail = "";
                    response.Response = user;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }

            return response;
        }
        public async Task<ServiceResponse<IBUser>> MordorGetUser(string username)
        {
            ServiceResponse<IBUser> response = new();
            try
            {
                var user = await _ibMordorDatabaseContext.User.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == username);
                if (user == null)
                {
                    response.StatusCode = 404;
                    response.Detail = "User Not Found";
                }
                else
                {
                    response.StatusCode = 200;
                    response.Detail = "";
                    response.Response = user;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }

            return response;
        }

        public async Task<ServiceResponse<UserInfo>> GetAmorphieUserFromDodge(string username)
        {
            var response = new ServiceResponse<UserInfo>();
            try
            {
                var userResponse = await GetUser(username);
                if (userResponse.StatusCode != 200)
                {
                    response.Detail = userResponse.Detail;
                    response.StatusCode = userResponse.StatusCode;
                    return response;
                }
                var user = userResponse.Response;


                var userStatus = await _ibDatabaseContext.Status.Where(s => s.UserId == user!.Id && (!s.State.HasValue || s.State.Value == 10)).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync();
                if (userStatus?.Type == 30 || userStatus?.Type == 40)
                {
                    response.Detail = "User Status is Not Active";
                    response.StatusCode = 480;
                    return response;
                }

                var passwordResponse = await GetPassword(user!.Id);
                if (passwordResponse.StatusCode != 200)
                {
                    response.Detail = userResponse.Detail;
                    response.StatusCode = userResponse.StatusCode;
                    return response;
                }
                var passwordRecord = passwordResponse.Response;

                var role = await _ibDatabaseContext.Role.Where(r => r.UserId.Equals(user!.Id) && r.Channel.Equals(10) && r.Status.Equals(10)).OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync();
                if(role is {} && (role.ExpireDate ?? DateTime.MaxValue) > DateTime.Now)
                {
                    var roleDefinition = await _ibDatabaseContext.RoleDefinition.FirstOrDefaultAsync(d => d.Id.Equals(role.DefinitionId) && d.IsActive);
                    if(roleDefinition is {})
                    {
                        if(roleDefinition.Key == 0)
                        {   
                            response.Detail = ErrorHelper.GetErrorMessage(LoginErrors.NotAuthorized, "en-EN");
                            response.StatusCode = 481;
                            return response;
                        }
                        else
                        {
                            _transactionService.RoleKey = roleDefinition.Key;
                        }
                    }
                }

                var userInfoResult = await _profileService.GetCustomerSimpleProfile(username!);
                if (userInfoResult.StatusCode != 200)
                {
                    response.Detail = "User Info Couldn't Be Fetched";
                    response.StatusCode = 482;
                    return response;
                }

                var userInfo = userInfoResult.Response;

                if (userInfo!.data!.profile!.Equals("customer") || !userInfo!.data!.profile!.status!.Equals("active"))
                {
                    response.Detail = "User is Not Customer or Active";
                    response.StatusCode = 483;
                    return response;
                }

                var mobilePhoneCount = userInfo!.data!.phones!.Count(p => p.type!.Equals("mobile"));
                if (mobilePhoneCount != 1)
                {
                    response.Detail = "Customer Should Have Only 1 Mobile Phone Number";
                    response.StatusCode = 483;
                    return response;
                }

                var mobilePhone = userInfo!.data!.phones!.FirstOrDefault(p => p.type!.Equals("mobile"));
                if (string.IsNullOrWhiteSpace(mobilePhone!.prefix) || string.IsNullOrWhiteSpace(mobilePhone!.number))
                {
                    response.Detail = "Customer Should Have 1 Valid Mobile Phone Number";
                    response.StatusCode = 483;
                    return response;
                }

                var userRequest = new UserInfo
                {
                    firstName = userInfo!.data.profile!.name!,
                    lastName = userInfo!.data.profile!.surname!,
                    phone = new core.Models.User.UserPhone()
                    {
                        countryCode = mobilePhone!.countryCode!,
                        prefix = mobilePhone!.prefix,
                        number = mobilePhone!.number
                    },
                    state = "Active",
                    salt = passwordRecord.Id.ToString(),
                    password = "123",
                    explanation = "Migrated From IB",
                    reason = "Amorphie Login",
                    isArgonHash = true
                };

                var verifiedMailAddress = userInfo.data.emails!.FirstOrDefault(m => m.isVerified == true);
                userRequest.eMail = verifiedMailAddress?.address ?? "";
                userRequest.reference = username!;

                response.StatusCode = 200;
                response.Response = userRequest;

                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();

                return response;
            }
            
        }

        public PasswordVerificationResult VerifyPassword(string hashedPassword, string providedPassword, string salt)
        {
            PasswordHasher hasher = new();
            return hasher.VerifyHashedPassword(hashedPassword, providedPassword, salt);
        }

    }
}