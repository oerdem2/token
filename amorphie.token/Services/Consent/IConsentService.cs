using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Consent;

namespace amorphie.token.Services.Consent
{
    public interface IConsentService
    {
        public Task<ServiceResponse<ConsentResponse>> GetConsent(Guid consentId);
    }
}