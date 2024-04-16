using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using amorphie.token.core;

namespace amorphie.token;

public class EkycProvider : BaseEkycProvider, IEkycProvider
{
    public EkycProvider(IHttpClientFactory httpClientFactory,
                        ILogger<BaseEkycProvider> logger,
                        IConfiguration configuration,
                        DaprClient daprClient) : base(httpClientFactory, logger, configuration, daprClient)
    {


    }

    public async Task<GetIntegrationInfoModels.Response> GetIntegrationInfoAsync(Guid reference)
    {
        var resourceUrl = $"Verify/Integration/Get";
        var headers = await GetEnquraHeadersAsync();
        var request = new GetIntegrationInfoModels.Request
        {
            Types = new List<string> { EkycConstants.Session },
            Reference = reference,
            IdentityType = string.Empty,
            IdentityNo = string.Empty,
            SessionUId = string.Empty,
            Statuses = new List<string>()
        };
        var httpClient = _httpClientFactory.CreateClient("Enqura");
        httpClient.DefaultRequestHeaders.Authorization = headers;
        StringContent jsonRequest = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var resp = await httpClient.PostAsync(resourceUrl, jsonRequest); //( resourceUrl, request, headers);
        var response = resp.Content.ReadFromJsonAsync<GetIntegrationInfoModels.Response>();
        // if (response == null || !resp.IsSuccessStatusCode)
        // {
        //     throw new EkycIntegrationException(BusinessExceptionKey.EkycIntegrationGetFailException, "enqura entegrasyon session get hatası", 455);
        // }
        // if (response.Data == null)
        // {
        //     throw new EkycIntegrationException(BusinessExceptionKey.EkycIntegrationGetBlankDataException, "enqura entegrasyon session data boş", 456);
        // }

        //  return response;
        return null;
    }

    public Task<GetSessionInfoModels.Response> GetSessionInfoAsync(Guid sessionId, bool loadContent = true, bool loadDetails = true)
    {
        throw new NotImplementedException();
    }

    public Task<EkycRegisterModels.Response> RegisterAsync(EkycRegisterModels.Request request)
    {
        throw new NotImplementedException();
    }
}
