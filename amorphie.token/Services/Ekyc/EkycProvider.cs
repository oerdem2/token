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
        var response = await resp.Content.ReadFromJsonAsync<GetIntegrationInfoModels.Response>();
        if (response is null || !response.IsSuccessful)
        {
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session get hatası");
        }

        if (response.Data is null)
        {
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session data boş");
        }

        return response;
    }

    public async Task<GetSessionInfoModels.Response> GetSessionInfoAsync(Guid sessionId, bool loadContent = true, bool loadDetails = true)
    {

        var resourceUrl = $"Verify/Session/Get";
        var headers = await GetEnquraHeadersAsync();
        var request = new GetSessionInfoModels.Request
        {
            SessionUId = sessionId.ToString(),
            LoadContent = loadContent,
            LoadDetails = loadDetails
        };

        var httpClient = _httpClientFactory.CreateClient("Enqura");
        httpClient.DefaultRequestHeaders.Authorization = headers;
        StringContent jsonRequest = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var resp = await httpClient.PostAsync(resourceUrl, jsonRequest); //( resourceUrl, request, headers);
        var response = await resp.Content.ReadFromJsonAsync<GetSessionInfoModels.Response>();

        if (response is null || !response.IsSuccessful)
        {
            _logger.LogError($"Enqura Service GetSessionInfo Error: response is null or response is not success!");
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session get hatası");
        }

        if (response.Data is null)
        {
            _logger.LogError($"Enqura Service GetSessionInfo Error: Response's data is null!");
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session data boş");
        }

        return response;

    }

    public async Task<EkycRegisterModels.Response> RegisterAsync(EkycRegisterModels.Request request)
    {
        var resourceUrl = $"Verify/Integration/Add";
        var headers = await GetEnquraHeadersAsync();

        var httpClient = _httpClientFactory.CreateClient("Enqura");
        httpClient.DefaultRequestHeaders.Authorization = headers;
        StringContent jsonRequest = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var resp = await httpClient.PostAsync(resourceUrl, jsonRequest); //( resourceUrl, request, headers);
        var response = await resp.Content.ReadFromJsonAsync<EkycRegisterModels.Response>();
        if (response is null || !response.IsSuccessful)
        {
            _logger.LogError($"Enqura Service RegisterAsync Error: session could not create or register");
            throw new ServiceException((int)resp.StatusCode, "enqura entegrasyon session register hatası");
        }
        return response;
    }

    public async Task TestEnqura()
    {
        var header = await GetEnquraHeadersAsync();
    }
}
