using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.token.core.Models.Consent;
using amorphie.token.Services.TransactionHandler;

namespace amorphie.token.Services.Consent
{
    public class ConsentServiceLocal : ServiceBase, IConsentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ConsentServiceLocal(ILogger<ConsentService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory) : base(logger, configuration)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ServiceResponse<DocumentResponse>> CheckDocument(string clientId, string roleId, string citizenshipNo)
        {
            var httpClient = _httpClientFactory.CreateClient("Consent");
            StringContent req = new StringContent("", System.Text.Encoding.UTF8, "application/json");

            var httpResponseMessage = await httpClient.PostAsync(
                $"Authorization/CheckAuthorizationForLogin/clientCode={clientId}&roleId={roleId}&userTCKN={citizenshipNo}?scope={citizenshipNo}", req);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return new ServiceResponse<DocumentResponse>() { StatusCode = 200, Response = JsonSerializer.Deserialize<DocumentResponse>(await httpResponseMessage.Content.ReadAsStringAsync()) };
            }
            else
            {
                return new ServiceResponse<DocumentResponse>() { StatusCode = (int)httpResponseMessage.StatusCode };
            }
        }

        public async Task<ServiceResponse> CheckConsent(string clientId, string roleId, string citizenshipNo)
        {
            var httpClient = _httpClientFactory.CreateClient("Consent");

            var httpResponseMessage = await httpClient.GetAsync(
                $"Authorization/CheckAuthorizationForLogin/clientCode={clientId}&roleId={roleId}&userTCKN={citizenshipNo}&scope={citizenshipNo}");

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return new ServiceResponse() { StatusCode = 200 };
            }
            else
            {
                return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode };
            }
        }

        public async Task<ServiceResponse> SaveConsent(string clientId, string roleId, string citizenshipNo)
        {
            var httpClient = _httpClientFactory.CreateClient("Consent");
            StringContent req = new StringContent(JsonSerializer.Serialize(new
            {
                roleId = roleId,
                clientCode = clientId,
                userTCKN = citizenshipNo,
                scope = citizenshipNo
            }), System.Text.Encoding.UTF8, "application/json");

            var httpResponseMessage = await httpClient.PostAsync(
                $"Authorization/AuthorizeForLogin", req);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return new ServiceResponse() { StatusCode = 200 };
            }
            else
            {
                return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode };
            }
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

        public async Task<ServiceResponse> UpdateConsentForUsage(Guid consentId)
        {
            var httpClient = _httpClientFactory.CreateClient("Consent");
            StringContent req = new StringContent(JsonSerializer.Serialize(new
            {
                id = consentId,
                state = "K"
            }), System.Text.Encoding.UTF8, "application/json");

            var httpResponseMessage = await httpClient.PostAsync(
                "OpenBankingConsentHHS/UpdateConsentStatusForUsage", req);
                
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return new ServiceResponse() { StatusCode = 200 };
            }
            else
            {
                return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode,Detail = await httpResponseMessage.Content.ReadAsStringAsync()};
            }
        }

        public async Task<ServiceResponse> CheckAuthorizationConsent(string clientId, string currentUser, string scopeUser)
        {
            var httpClient = _httpClientFactory.CreateClient("Consent");

            var httpResponseMessage = await httpClient.GetAsync(
                $"Authorization/CheckConsent/clientCode={clientId}&userTCKN={currentUser}&scope={scopeUser}");

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return new ServiceResponse() { StatusCode = 200 };
            }
            else
            {
                return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode };
            }
        }

        public async Task<ServiceResponse> UpdateConsentInOtp(Guid consentId, string citizenshipNo)
        {
            var httpClient = _httpClientFactory.CreateClient("Consent");
            StringContent req = new StringContent(JsonSerializer.Serialize(new
            {
                id = consentId,
                userTckn = citizenshipNo
            }), System.Text.Encoding.UTF8, "application/json");

            var httpResponseMessage = await httpClient.PostAsync(
                "OpenBankingConsentHHS/UpdateConsentInOtp", req);
                
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return new ServiceResponse() { StatusCode = 200 };
            }
            else
            {
                return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode,Detail = await httpResponseMessage.Content.ReadAsStringAsync()};
            }
        }

        public async Task<ServiceResponse> CheckAuthorizeForInstitutionConsent(Guid consentId, string citizenshipNo)
        {
            var httpClient = _httpClientFactory.CreateClient("Consent");
            StringContent req = new StringContent("", System.Text.Encoding.UTF8, "application/json");
            
            var httpResponseMessage = await httpClient.SendAsync(new HttpRequestMessage(){Method=HttpMethod.Post, RequestUri = new Uri(Configuration["ConsentBaseAddress"]+"OpenBankingConsentHHS/CheckAuthorizeForInstitutionConsent?consentId="+consentId+"&tckn="+citizenshipNo)});
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return new ServiceResponse() { StatusCode = 200 };
            }
            else
            {
                return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode,Detail = await httpResponseMessage.Content.ReadAsStringAsync()};
            }
        }

        public async Task<ServiceResponse> CancelConsent(Guid consentId, string cancelDetailCode)
        {
            var httpClient = _httpClientFactory.CreateClient("Consent");
            StringContent content = new StringContent(            
            JsonSerializer.Serialize(new
            {
                consentId = consentId,
                cancelDetailCode = cancelDetailCode
            }), System.Text.Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri(Configuration["ConsentBaseAddress"]+"OpenBankingConsentHHS/Cancel"),
                Content = content
            };

            var httpResponseMessage = await httpClient.SendAsync(
                request);
                
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return new ServiceResponse() { StatusCode = 200 };
            }
            else
            {
                return new ServiceResponse() { StatusCode = (int)httpResponseMessage.StatusCode,Detail = await httpResponseMessage.Content.ReadAsStringAsync()};
            }
        }
    }
}