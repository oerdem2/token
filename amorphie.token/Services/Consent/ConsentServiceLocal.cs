using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Consent;

namespace amorphie.token.Services.Consent
{
    public class ConsentServiceLocal : ServiceBase,IConsentService
    {
        private readonly DaprClient _daprClient;
        private readonly IHttpClientFactory _httpClientFactory;
        public ConsentServiceLocal(ILogger<ConsentService> logger,IConfiguration configuration,DaprClient daprClient,IHttpClientFactory httpClientFactory) : base(logger,configuration)
        {
            _daprClient = daprClient;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ServiceResponse<ConsentResponse>> GetConsent(Guid consentId)
        {
            var httpClient = _httpClientFactory.CreateClient("Consent");
            var httpResponseMessage = await httpClient.GetAsync(
                "OpenBankingConsentHHS/" + consentId.ToString());

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var consentResponse = await httpResponseMessage.Content.ReadFromJsonAsync<ConsentResponse>();
                if (consentResponse == null)
                {
                    throw new ServiceException((int)Errors.InvalidUser, "Consent not found with provided info");
                }
                return new ServiceResponse<ConsentResponse>() { StatusCode = 200, Response = consentResponse };
            }
            else
            {
                throw new ServiceException((int)Errors.InvalidUser, "Consent Endpoint Did Not Response Successfully");
            }
        }
    }
}