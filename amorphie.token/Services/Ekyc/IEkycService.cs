using amorphie.token.core;

namespace amorphie.token;

public interface IEkycService
{
    /// <summary>
    /// Create enqura session 
    /// </summary>
    /// <returns></returns>
    Task<EkycCreateSessionResultModel> CreateSession(Guid instanceId, string reference, EkycProcess.ProcessType processType);

    /// <summary>
    /// Get integration info using session Id 
    /// </summary>
    /// <param name="referenceId"></param>
    /// <returns></returns>
    Task<GetIntegrationInfoModels.Response> GetSessionByIntegrationReferenceAsync(Guid referenceId);
    /// <summary>
    /// Get session info using session Id
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="loadContent"></param>
    /// <param name="loadDetails"></param>
    /// <returns></returns>
    Task<GetSessionInfoModels.Response> GetSessionInfoAsync(Guid sessionId, bool loadContent = true, bool loadDetails = true);
}
