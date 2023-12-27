
using amorphie.token.core.Models.Consent;
using amorphie.token.core.Models.Profile;
using amorphie.token.core.Models.Transaction;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.Profile;

namespace amorphie.token.Services.TransactionHandler
{
    public class TransactionService : ServiceBase,ITransactionService
    {
        private readonly IProfileService _profileService;
        private readonly IInternetBankingUserService _internetBankingUserService;
        private readonly IUserService _userService;
        private readonly DaprClient _daprClient;
        private Transaction? _transaction;
        public Transaction? Transaction => _transaction;

        private string _ip;
        public string IpAddress { get => _ip; set => _ip = value; }

        public TransactionService(ILogger<TransactionService> logger, IConfiguration configuration,
        IProfileService profileService,IInternetBankingUserService internetBankingUserService,IUserService userService,DaprClient daprClient) : base(logger, configuration)
        {
            _profileService = profileService;
            _internetBankingUserService = internetBankingUserService;
            _userService = userService;
            _daprClient = daprClient;
        }

        // public async Task GetTransactionInterval()
        // {
        //     await Task.Run(async() => {
        //         while(true)
        //         {
        //             _transaction = await _daprClient.GetStateAsync<core.Models.Transaction.Transaction>(Configuration["DAPR_STATE_STORE_NAME"],"txn_"+_transaction!.Id.ToString());
        //             await Task.Delay(700);
        //         }  
        //     });
        // }

        public async Task<ServiceResponse> GetTransaction(Guid id)
        {
            var response = new ServiceResponse();
            try
            {
                _transaction = await _daprClient.GetStateAsync<core.Models.Transaction.Transaction>(Configuration["DAPR_STATE_STORE_NAME"],"txn_"+id.ToString());
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }
            
            return response;
        }

        public async Task<ServiceResponse> ReloadTransaction()
        {
            var response = new ServiceResponse();
            try
            {
                _transaction = await _daprClient.GetStateAsync<core.Models.Transaction.Transaction>(Configuration["DAPR_STATE_STORE_NAME"],"txn_"+_transaction!.Id.ToString());
                Console.WriteLine("Reload Transaction Next Event: "+_transaction.TransactionNextEvent);
                Console.WriteLine("Reload Transaction Next Page : "+_transaction.TransactionNextPage);
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }
            
            return response;
        }


        public async Task<ServiceResponse> SaveTransaction(core.Models.Transaction.Transaction transaction)
        {
            var response = new ServiceResponse();
            try
            {
                await _daprClient.SaveStateAsync<core.Models.Transaction.Transaction>(Configuration["DAPR_STATE_STORE_NAME"],"txn_"+transaction.Id.ToString(),transaction);
                _transaction = transaction;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }

            return response;
        }

        private async Task<ServiceResponse> SaveTransaction()
        {
            var response = new ServiceResponse();
            try
            {
                await _daprClient.SaveStateAsync<core.Models.Transaction.Transaction>(Configuration["DAPR_STATE_STORE_NAME"],"txn_"+_transaction!.Id.ToString(),_transaction);
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }

            return response;
        }

        public async Task<ServiceResponse> CheckLogin(string username, string password)
        {
            ServiceResponse response = new();
            var userResponse = await _internetBankingUserService.GetUser(username);
            if(userResponse.StatusCode != 200)
            {
                response.StatusCode = userResponse.StatusCode;
                response.Detail = userResponse.Detail;
                return response;
            }
            var user = userResponse.Response;

            var passwordResponse = await _internetBankingUserService.GetPassword(user!.Id);
            if(userResponse.StatusCode != 200)
            {
                response.StatusCode = userResponse.StatusCode;
                response.Detail = userResponse.Detail;
                return response;
            }
            var passwordRecord = passwordResponse.Response;

            var isVerified = _internetBankingUserService.VerifyPassword(passwordRecord!.HashedPassword,password,passwordRecord.Id.ToString());
            //Consider SuccessRehashNeeded
            if(isVerified != PasswordVerificationResult.Success)
            {
                response.StatusCode = 401;
                response.Detail = "Username or password don't match";
                return response;
            }

            var migrateResult = await MigrateUser(username,password,passwordRecord.Id.ToString());
            if(migrateResult.StatusCode != 200)
            {
                response.StatusCode = migrateResult.StatusCode;
                response.Detail = migrateResult.Detail;
                return response;
            }

            response.StatusCode = 200;
            response.Detail = "Success";
            return response;
        }

        public async Task<ServiceResponse> CheckLoginFromWorkflow(string username, string password)
        {
            ServiceResponse response = new();
            var userResponse = await _internetBankingUserService.GetUser(username);
            if(userResponse.StatusCode != 200)
            {
                response.StatusCode = userResponse.StatusCode;
                response.Detail = userResponse.Detail;
                return response;
            }
            var user = userResponse.Response;

            var passwordResponse = await _internetBankingUserService.GetPassword(user!.Id);
            if(userResponse.StatusCode != 200)
            {
                response.StatusCode = userResponse.StatusCode;
                response.Detail = userResponse.Detail;
                return response;
            }
            var passwordRecord = passwordResponse.Response;

            var isVerified = _internetBankingUserService.VerifyPassword(passwordRecord!.HashedPassword,password,passwordRecord.Id.ToString());
            //Consider SuccessRehashNeeded
            if(isVerified != PasswordVerificationResult.Success)
            {
                response.StatusCode = 401;
                response.Detail = "Username or password don't match";
                return response;
            }

            var migrateResult = await MigrateUser(username,password,passwordRecord.Id.ToString(),_transaction!.Profile);
            if(migrateResult.StatusCode != 200)
            {
                response.StatusCode = migrateResult.StatusCode;
                response.Detail = migrateResult.Detail;
                return response;
            }

            var userResult = await _userService.Login(new LoginRequest(){
                Reference = username,
                Password = password
            });
            if(userResult.StatusCode != 200)
            {
                response.StatusCode = userResult.StatusCode;
                response.Detail = userResult.Detail;
                return response;
            }

            _transaction.User = userResult.Response;
            await SaveTransaction();

            response.StatusCode = 200;
            response.Detail = "Success";
            return response;
        }

        private async Task<ServiceResponse> MigrateUser(string username,string password,string salt,ProfileResponse? profile = null)
        {
            var response = new ServiceResponse();

            ServiceResponse<ProfileResponse>? userInfoResult = null;
            if(profile == null)
            {
                userInfoResult = await _profileService.GetCustomerProfile(username);
                if(userInfoResult.StatusCode != 200)
                {
                    response.StatusCode = userInfoResult.StatusCode;
                    response.Detail = userInfoResult.Detail;
                    return response;
                }
            }
            
            var userInfo = profile ?? userInfoResult!.Response;

            var userRequest = new UserInfo
            {
                firstName = userInfo!.customerName,
                lastName = userInfo.surname
            };
            var phone = userInfo.phones.FirstOrDefault(p => p.type == "mobile");
            userRequest.phone = new core.Models.User.UserPhone(){
                countryCode = phone!.countryCode,
                prefix = phone.prefix,
                number = phone.number
            };
            userRequest.state = "Active";
            userRequest.salt = salt;
            userRequest.password = password;
            userRequest.explanation = "Migrated From IB";
            userRequest.reason = "Amorphie Login";
            userRequest.isArgonHash = true;
            var verifiedMailAddress = userInfo.emails.FirstOrDefault(m => m.isVerified == true);
            userRequest.eMail = verifiedMailAddress?.address ?? "";
            userRequest.reference = username;

            var migrateResult = await _userService.SaveUser(userRequest);
            if(migrateResult.StatusCode != 200)
            {
                response.StatusCode = migrateResult.StatusCode;
                response.Detail = migrateResult.Detail;
                return response;
            }

            _transaction!.UserInfo = userRequest;
            await SaveTransaction();

            response.StatusCode = 200;
            response.Detail = "Success";
            return response;
        }

        public ServiceResponse<LoginResponse> GetUser()
        {
            if(_transaction!.User == null)
            {
                return new ServiceResponse<LoginResponse>()
                {
                    StatusCode = 404,
                    Detail = "Trasanction Does Not Contains User"
                };
            }

            return new ServiceResponse<LoginResponse>()
            {
                StatusCode = 200,
                Response = _transaction.User                    
            };
        }

        public ServiceResponse<ConsentResponse> GetConsent()
        {
            if(_transaction!.ConsentData == null)
            {
                return new ServiceResponse<ConsentResponse>()
                {
                    StatusCode = 404,
                    Detail = "Trasanction Does Not Contains User"
                };
            }

            return new ServiceResponse<ConsentResponse>()
            {
                StatusCode = 200,
                Response = _transaction.ConsentData                    
            };
        }

        public async Task<ServiceResponse> SaveUser(LoginResponse user)
        {
            _transaction!.User = user;
            await SaveTransaction();
            return new ServiceResponse(){
                StatusCode = 200
            };
        }

        public async Task<ServiceResponse> SaveConsent(ConsentResponse consent)
        {
            _transaction!.ConsentData = consent;
            await SaveTransaction();
            return new ServiceResponse(){
                StatusCode = 200
            };
        }
    }
}