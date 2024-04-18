
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using amorphie.core.Extension;
using amorphie.token.core;


namespace amorphie.token;

public class BaseEkycProvider
{
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly ILogger<BaseEkycProvider> _logger;
    private readonly IConfiguration _configuration;
    private readonly DaprClient _daprClient;

    public BaseEkycProvider(IHttpClientFactory httpClientFactory,
                            ILogger<BaseEkycProvider> logger,
                            IConfiguration configuration,
                            DaprClient daprClient)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        _daprClient = daprClient;
    }

    protected async Task<AuthenticationHeaderValue> GetEnquraHeadersAsync()
    {
        //     var headers = new Dictionary<string, string>
        //  {
        //      { "Authorization", "Bearer " + await GetTokenAsync() }
        //  };
        var header = new AuthenticationHeaderValue("Bearer", await GetTokenAsync());
        return header;
    }

    protected async Task<string> GetTokenAsync()
    {
        var token = await _daprClient.GetStateAsync<string>(_configuration["DAPR_STATE_STORE_NAME"], "amorphie-enquraToken");
        if (token is null)
        {
            var resourceUrl = $"Auth/Token";

            var request = new EkycTokenModels.Request
            {
                UserName = _configuration["EnquraUsername"]!,
                Password = _configuration["EnquraPassword"]!,
            };

            var httpClient = _httpClientFactory.CreateClient("Enqura");
            StringContent tokenRequest = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(resourceUrl, tokenRequest);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Enqura token service request fail!");
                throw new Exception($"Enqura token request result: {response.StatusCode} - token service request error.");
            }

            var resp = await response.Content.ReadFromJsonAsync<EkycTokenModels.Response>();
            if (resp?.IsSuccessful != true)
            {
                _logger.LogError($"Enqura token service LOGIN fail!");
                throw new Exception($"Enqura token LOGIN result: {response.StatusCode} - token service LOGIN error.");
            }

            await _daprClient.SaveStateAsync<string>(_configuration["DAPR_STATE_STORE_NAME"], "amorphie-enquraToken", resp.Token);
            token = resp.Token;

        }

        return token;
    }
}
