
using amorphie.token.core.Models.Profile;
using Refit;

namespace amorphie.token.Services.Profile
{
    public class ProfileService : ServiceBase,IProfileService
    {
        private readonly IProfile _profile;
        private ServiceResponse<ProfileResponse>? _profileResponse;
        public ProfileService(ILogger<ProfileService> logger, IConfiguration configuration,IProfile profile) : base(logger, configuration)
        {
            _profile = profile;
            _profileResponse = null;
        }

        public async Task<ServiceResponse<ProfileResponse>> GetCustomerProfile(string reference)
        {
            if(_profileResponse != null)
                return _profileResponse;

            var result = new ServiceResponse<ProfileResponse>();
            try
            {
                var apiResponse = await _profile.GetProfile(reference,Configuration["ProfileUser"]!,Configuration["ProfileChannel"]!,Configuration["ProfileBranch"]!);

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

            _profileResponse = result;
            return result;
        }
    }
}