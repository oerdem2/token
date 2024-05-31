using amorphie.token.core;
using amorphie.token.core.Models.Profile;
using static amorphie.token.core.EkycMevduatStatusCheckModels;

namespace amorphie.token;

public interface IEkycService
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<EkycCreateSessionResultModel> CreateSession(Guid instanceId, string citizenshipNumber, string callType, bool hasWfId);

    /// <summary>
    /// Get integration info using session Id 
    /// </summary>
    /// <param name="referenceId"></param>
    /// <returns></returns>
    Task<GetIntegrationInfoModels.Data> GetSessionByIntegrationReferenceAsync(Guid referenceId);
    /// <summary>
    /// Get session info using session Id
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="loadContent"></param>
    /// <param name="loadDetails"></param>
    /// <returns></returns>
    Task<GetSessionInfoModels.Response> GetSessionInfoAsync(Guid sessionId, bool loadContent = true, bool loadDetails = true);
    /// <summary>
    /// Convert to callType request to System Const name
    /// </summary>
    /// <param name="callType"></param>
    /// <returns></returns>
    string GetCallType(string callType);

    /// <summary>
    /// Get video call results for mevduat_on or mevduat_HepsiBurada
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<ServiceResponse<EkycMevduatStatusCheckModels.Response>> CheckCallStatusForMevduat(EkycMevduatStatusCheckModels.Request request); 
}
