using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Consent;

namespace amorphie.token.Services.Consent
{
    public class ConsentService : ServiceBase, IConsentService
    {
        private readonly DaprClient _daprClient;
        public ConsentService(ILogger<ConsentService> logger,IConfiguration configuration,DaprClient daprClient) : base(logger,configuration)
        {
            _daprClient = daprClient;
        }

        public async Task<ServiceResponse<ConsentResponse>> GetConsent(Guid consentId)
        {
            try
            {
                var consent = await _daprClient.InvokeMethodAsync<ConsentResponse>(HttpMethod.Get, Configuration["ConsentServiceAppName"], "/OpenBankingConsentHHS/"+consentId.ToString());
                
                return new ServiceResponse<ConsentResponse>()
                {
                    StatusCode = 200,
                    Detail = "",
                    Response = consent
                };
            }
            catch (InvocationException ex)
            {
                return new ServiceResponse<ConsentResponse>()
                {
                    StatusCode = (int)ex.Response.StatusCode,
                    Detail = await ex.Response.Content.ReadAsStringAsync()
                };
            }
            catch (System.Exception ex)
            {
                return new ServiceResponse<ConsentResponse>()
                {
                    StatusCode = 500,
                    Detail = ex.ToString()
                };
            }
        }

    }
}