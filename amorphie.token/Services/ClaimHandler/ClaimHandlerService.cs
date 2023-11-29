using System.Security.Claims;
using amorphie.token.core.Models.Consent;
using amorphie.token.Services.TransactionHandler;

namespace amorphie.token.Services.ClaimHandler
{
    public class ClaimHandlerService : ServiceBase, IClaimHandlerService
    {
        private readonly ITransactionService _transactionService;
        private readonly ITagService _tagService;

        private  LoginResponse? _user;
        private  ConsentResponse? _consent;

        //Using For Tag Service Query Params
        private  string? _queryStringForTag;
        public ClaimHandlerService(
            ILogger<ClaimHandlerService> logger,IConfiguration configuration,
            ITransactionService transactionService,ITagService tagService)
            :base(logger,configuration)
        {
            _transactionService = transactionService;
            _tagService = tagService;

        }

        private void SetUser()
        {
            var userResult = _transactionService.GetUser();
            if(userResult.StatusCode != 200)
            {
                _queryStringForTag = string.Empty;
                _user = null;
            }
            else
            {
                _user = userResult.Response;
                _queryStringForTag = string.Empty;
                _queryStringForTag += "?reference=" + _user!.Reference;
                _queryStringForTag += "&mail=" + _user!.EMail;
                _queryStringForTag += "&phone=" + _user!.MobilePhone!.ToString();
            }
        }

        private void SetConsent()
        {
            var consentResult = _transactionService.GetConsent();
            if(consentResult.StatusCode != 200)
            {
                _consent = null;
            }
            else
            {
                _consent = consentResult.Response;
            }
        } 

        public async Task<Claim?> GetClaimDetail(string[] claimPath)
        {
            if (claimPath.First().Equals("tag"))
            {
                try
                {
                    var domain = claimPath[1];
                    var entity = claimPath[2];
                    var tagName = claimPath[3];
                    var fieldName = claimPath[4];

                    var tagData = await _tagService.GetTagInfo(domain, entity, tagName, _queryStringForTag!);
                    if (tagData == null)
                        return null;

                    return new Claim(string.Join('.', claimPath), tagData[fieldName].ToString());

                }
                catch (Exception ex)
                {
                    Logger.LogError("Get Tag Info :" + ex.ToString());
                }
            }
            
            if (claimPath.First().Equals("user"))
            {
                if(_user == null)
                    return null;

                Type t = _user!.GetType();

                var property = t.GetProperties().FirstOrDefault(p => p.Name.ToLower() == claimPath[1]);
                if (property == null)
                    return null;
                if(property!.GetValue(_user!) == null)
                    return null;

                return new Claim(string.Join('.', claimPath), property!.GetValue(_user!)!.ToString()!);
            }

            if (claimPath.First().Equals("openbanking"))
            {
                if(_consent == null)
                    return null;

                Type t = _consent!.GetType();

                var property = t.GetProperties().FirstOrDefault(p => p.Name.ToLower() == claimPath[2]);
                if (property == null)
                    return null;
                if (property.GetValue(_consent) == null)
                    return null;

                return new Claim(string.Join('.', claimPath), property!.GetValue(_consent)!.ToString()!);
            }
            

            return null;
        }

        public async Task<List<Claim>> PopulateClaims(List<string> clientClaims)
        {
            SetUser();
            SetConsent();
            
            List<Claim> claims = new List<Claim>();
            
            foreach (var identityClaim in clientClaims)
            {
                var claimDetail = identityClaim.Split("||");
                var alternativeClaim = String.Empty;

                string? primaryClaim;
                if (claimDetail.Length == 1)
                {
                    primaryClaim = claimDetail.First();
                }
                else
                {
                    primaryClaim = claimDetail.First();
                    alternativeClaim = claimDetail[1];
                }

                var claimInfo = primaryClaim.Split(".");

                var claimValue = await GetClaimDetail(claimInfo);

                if (claimValue != null)
                {
                    claims.Add(claimValue);
                }
                else
                {
                    claimInfo = alternativeClaim.Split(".");
                    claimValue = await GetClaimDetail(claimInfo);
                    if (claimValue != null)
                    {
                        claims.Add(claimValue);
                    }
                }
            }

            return claims;
        }
    }
}