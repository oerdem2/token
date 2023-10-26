
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.core.Models.Profile;

namespace amorphie.token.Services.Profile
{
    public interface IProfileService
    {
        public Task<ServiceResponse<ProfileResponse>> GetCustomerProfile(string reference);
    }
}