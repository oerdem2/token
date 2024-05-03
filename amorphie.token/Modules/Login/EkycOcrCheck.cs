using System.Dynamic;
using amorphie.token.core;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public static class EkycOcrCheck
public static class EkycOcrCheck
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> Check(
        [FromBody] dynamic body,
        [FromServices] IEkycService ekycService
    )
    {

        var transitionName = body.GetProperty("LastTransition").ToString();
        // var transactionId = body.GetProperty("InstanceId").ToString();
        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());


        var ocrIsSuccess = dataChanged.entityData.IsSuccess;

        bool identityNoCompatible = false;
        bool ocrStatus = false;
        bool hasNfc = false;
        dynamic variables = new Dictionary<string, dynamic>();
        dataChanged.additionalData = new ExpandoObject();
        bool showHomePageButton = true;
        bool showTyrAgainButton = true;

        #region GetSession after connection state
        var referenceId = body.GetProperty("ReferenceId")?.ToString();
        var sessionIntegrationInfo = await ekycService.GetSessionByIntegrationReferenceAsync(Guid.Parse(referenceId));
        var sessionId = "";
        if (sessionIntegrationInfo.IsSuccessful && sessionIntegrationInfo.Data is not null && sessionIntegrationInfo.Data.Count > 0)
        {
            // Get SessionId 
            sessionId = sessionIntegrationInfo.Data.FirstOrDefault().SessionUId.ToString();


        }
        variables.Add("SessionId", sessionId);
        #endregion

        if (ocrIsSuccess && !string.IsNullOrEmpty(sessionId))
        {

            var session = await ekycService.GetSessionInfoAsync(Guid.Parse(sessionId));
            var identityNo = body.GetProperty("UserName").ToString();

            if (session is not null && session.Data is not null)
            {

                identityNoCompatible = identityNo == session.Data.IDDoc.IdentityNo;
                if (identityNoCompatible && session.Data.IDDoc.IsValid is true)
                {
                    // Set OK
                    hasNfc = session.Data.NFCExists;
                    dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
                    dataChanged.additionalData.OcrReadSuccess = true;
                    ocrStatus = true;


                }

            }

        }

        var ocrCurrentFailedCount = Convert.ToInt32(body.GetProperty("CurrentOcrFailedCount").ToString());
        if (!ocrStatus)
        {

            if (ocrCurrentFailedCount >= EkycConstants.OcrFailedMaxTryCount)
            {
                showTyrAgainButton = false;
            }
            ocrCurrentFailedCount++;
        }

        dataChanged.additionalData.ShowHomePageButton = showHomePageButton;
        dataChanged.additionalData.ShowTryAgainButton = showTyrAgainButton;


        // dynamic variables = new ExpandoObject();
        variables.Add("Init", true);
        variables.Add("OcrStatus", true);
        variables.Add("CurrentOcrFailedCount", ocrCurrentFailedCount);
        variables.Add("HasNfc",hasNfc);

        return Results.Ok(variables);

    }
}
