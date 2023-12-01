
using System.Security.Claims;

namespace amorphie.token.Services.ClaimHandler
{
    public interface IClaimHandlerService
    {
        public Task<List<Claim>>  PopulateClaims(List<string> clientClaims,LoginResponse? user);
    }
}