using System.Reflection;
using System.Security.Claims;
using amorphie.token.core.Models.Consent;
using amorphie.token.core.Models.Profile;
using amorphie.token.Services.TransactionHandler;

namespace amorphie.token.Services.ClaimHandler
{
    public class ClaimHandlerService : ServiceBase, IClaimHandlerService
    {
        private readonly ITransactionService _transactionService;
        private readonly ITagService _tagService;

        private LoginResponse? _user;
        private ConsentResponse? _consent;
        private SimpleProfileResponse? _profile;

        //Using For Tag Service Query Params
        private string? _queryStringForTag;
        public ClaimHandlerService(
            ILogger<ClaimHandlerService> logger, IConfiguration configuration,
            ITransactionService transactionService, ITagService tagService)
            : base(logger, configuration)
        {
            _transactionService = transactionService;
            _tagService = tagService;

        }

        public async Task<Claim?> GetClaimDetail(string[] claimPath)
        {
            var claimName = string.Empty;
            var claimType = string.Empty;
            if (claimPath.First().Contains("|"))
            {
                claimName = claimPath.First().Split("|")[0];
                claimType = claimPath.First().Split("|")[1];
                claimPath[0] = claimPath.First().Split("|")[1];
            }
            else
            {
                claimName = string.Join('.', claimPath);
                claimType = claimPath.First();
            }

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

                    return new Claim(claimName, tagData[fieldName].ToString());

                }
                catch (Exception ex)
                {
                    Logger.LogError("Get Tag Info :" + ex.ToString());
                }
            }

            if (claimPath.First().Equals("user"))
            {
                if (_user == null)
                    return null;

                Type t = _user!.GetType();

                var property = t.GetProperties().FirstOrDefault(p => p.Name.ToLower() == claimPath[1]);
                if (property == null)
                    return null;
                if (property!.GetValue(_user!) == null)
                    return null;

                return new Claim(claimName, property!.GetValue(_user!)!.ToString()!);
            }

            if (claimPath.First().Equals("openbanking"))
            {
                if (_consent == null)
                    return null;

                Type t = _consent!.GetType();

                var property = t.GetProperties().FirstOrDefault(p => p.Name.ToLower() == claimPath[2]);
                if (property == null)
                    return null;
                if (property.GetValue(_consent) == null)
                    return null;

                return new Claim(claimName, property!.GetValue(_consent)!.ToString()!);
            }

            if (claimPath.First().Equals("profile"))
            {
                if (_profile == null)
                    return null;

                Type t = _profile!.GetType();

                var propValue = GetPropertyValue(_profile, string.Join('.', claimPath.ToList().Skip(1)));

                if (propValue != null)
                    return new Claim(claimName, propValue);
                else
                    return null;

            }

            return null;
        }

        private string? GetPropertyValue(object src, string propName)
        {
            if (src == null) throw new ArgumentException("Value cannot be null.", "src");
            if (propName == null) throw new ArgumentException("Value cannot be null.", "propName");

            if (propName.Contains("."))
            {
                var temp = propName.Split(new char[] { '.' }, 2);
                var property = src.GetType().GetProperties().FirstOrDefault(p => p.Name.ToLower() == temp[0].ToLower());
                if (property == null)
                    return null;
                src = property.GetValue(src);
                return GetPropertyValue(src, temp[1]);
            }
            else
            {
                var property = src.GetType().GetProperties().FirstOrDefault(p => p.Name.ToLower() == propName.ToLower());

                return property != null ? property.GetValue(src, null).ToString() : null;
            }
        }
        public async Task<List<Claim>> PopulateClaims(List<string> clientClaims, LoginResponse? user, SimpleProfileResponse? profile = null, ConsentResponse? consent = null)
        {
            _profile = profile;
            if (consent != null)
            {
                _consent = consent;
            }

            if (user == null)
            {

            }
            else
            {
                _user = user;
                _queryStringForTag = string.Empty;
                _queryStringForTag += "?reference=" + _user!.Reference;
                _queryStringForTag += "&mail=" + _user!.EMail;
                _queryStringForTag += "&phone=" + _user!.MobilePhone!.ToString();
            }

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