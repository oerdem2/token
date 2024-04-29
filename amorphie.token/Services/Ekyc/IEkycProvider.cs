using amorphie.token.core;

namespace amorphie.token;

public interface IEkycProvider
{
    public Task<EkycRegisterModels.Response> RegisterAsync(EkycRegisterModels.Request request);
    public Task<GetSessionInfoModels.Response> GetSessionInfoAsync(Guid sessionId, bool loadContent = true, bool loadDetails = true);
    public Task<GetIntegrationInfoModels.Response> GetIntegrationInfoAsync(Guid reference);

    public Task TestEnqura();
}
