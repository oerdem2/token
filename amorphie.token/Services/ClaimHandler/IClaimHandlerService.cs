
using System.Security.Claims;
using amorphie.token.core.Models.Profile;

namespace amorphie.token.Services.ClaimHandler
{
    public interface IClaimHandlerService
    {
        public Task<List<Claim>>  PopulateClaims(List<string> clientClaims,LoginResponse? user,SimpleProfileResponse? userInfo = null);
    }
}