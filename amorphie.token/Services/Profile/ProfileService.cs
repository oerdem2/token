
using amorphie.core.Base;
using amorphie.token.core.Models.Profile;
using Refit;

namespace amorphie.token.Services.Profile
{
    public class ProfileService : ServiceBase,IProfileService
    {
        private readonly IProfile _profile;
        public ProfileService(ILogger logger, IConfiguration configuration,IProfile profile) : base(logger, configuration)
        {
            _profile = profile;
        }

        public async Task<ServiceResponse<ProfileResponse>> GetCustomerProfile(string reference)
        {
            var result = new ServiceResponse<ProfileResponse>();
            try
            {
                var apiResponse = await _profile.GetProfile(reference,Configuration["ProfileUser"],Configuration["ProfileChannel"],Configuration["ProfileBranch"]);

                result.Response = apiResponse;
                result.StatusCode = 200;
            }
            catch(ApiException ex)
            {
                result.StatusCode = (int)ex.StatusCode;
                result.Detail = ex.ToString();
            }
            catch (Exception ex)
            {
                result.StatusCode = 500;
                result.Detail = ex.ToString();
            }
            return result;
        }
    }
}