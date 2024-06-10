
using System.Security.Claims;
using amorphie.token.core.Models.Consent;
using amorphie.token.core.Models.Profile;

namespace amorphie.token.Services.ClaimHandler
{
    public interface IClaimHandlerService
    {
        public Task<List<Claim>> PopulateClaims(List<string> clientClaims, LoginResponse? user, SimpleProfileResponse? profile = null, ConsentResponse? consent = null,core.Models.Collection.User? collectionUser = null);
        public Task<List<KeyValuePair<string,object>?>> PopulatePrivateClaims(List<string> clientClaims, LoginResponse? user, SimpleProfileResponse? profile = null, ConsentResponse? consent = null,core.Models.Collection.User? collectionUser = null);

    }
}