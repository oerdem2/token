using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.LegacySSO;

namespace amorphie.token.Services.LegacySSO
{
    public interface ILegacySSOService
    {
        public Task<ServiceResponse<List<SsoCredential>>> GetSsoCredentials(string username, string appCode, string type);
    }
}