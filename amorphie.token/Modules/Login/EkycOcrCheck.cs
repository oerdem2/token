using System.Dynamic;
using amorphie.token.core;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;


public static class EkycOcrCheck
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> Check(
        [FromBody] dynamic body,
        [FromServices] IEkycService ekycService
    )
    {

        var transitionName = body.GetProperty("LastTransition").ToString();
        var transactionId = body.GetProperty("InstanceId").ToString();

        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());
        dynamic targetObject = new System.Dynamic.ExpandoObject();
        targetObject.Data = dataChanged;

        var ocrIsSuccess = dataChanged.entityData.IsSuccess;


        bool identityNoCompatible = false;
        bool ocrStatus = false;
        bool hasNfc = dataChanged.entityData.HasNfc;
        dynamic variables = new Dictionary<string, dynamic>();
        dataChanged.additionalData = new ExpandoObject();

        var callType = body.GetProperty("CallType").ToString();
        var instance = body.GetProperty("Instance").ToString();
        // var name = body.GetProperty("Name").ToString();
        // var surname = body.GetProperty("Surname").ToString();
        dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
        dataChanged.additionalData.callType = callType;
        var ApplicantFullName = body.GetProperty("ApplicantFullName").ToString();
        dataChanged.additionalData.applicantFullName = ApplicantFullName;
        // dataChanged.additionalData.customerName = name; // bu kısımları doldur.
        // dataChanged.additionalData.customerSurname = surname;
        dataChanged.additionalData.instanceId = instance;

        var sessionId = "";


        #region GetSession after connection state
        try
        {
            GetIntegrationInfoModels.Data sessionIntegrationInfo = await ekycService.GetSessionByIntegrationReferenceAsync(Guid.Parse(instance));

            if (sessionIntegrationInfo is not null && sessionIntegrationInfo.SessionUId is not null)
            {
                // Get SessionId 
                sessionId = sessionIntegrationInfo.SessionUId?.ToString();
            }
        }
        catch (Exception ex)
        {
            ocrStatus = false;
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
        if (!ocrStatus || !ocrIsSuccess)
        {

            // if (ocrCurrentFailedCount >= EkycConstants.OcrFailedMaxTryCount)
            // {
            //     showTyrAgainButton = false;
            // }
            ocrCurrentFailedCount++;
            variables.Add("FailedStepName", "ocr");
            // ocr failed


            if (!ocrStatus && ocrIsSuccess)
            {
                dataChanged.additionalData.pages = new List<EkycPageModel>
                {
                    EkycAdditionalDataContstants.StandartItem,
                    EkycAdditionalDataContstants.OcrFailedItemForIdentityMatch
                };
            }

            if (!ocrIsSuccess)
            {
                dataChanged.additionalData.pages = new List<EkycPageModel>
                {
                    EkycAdditionalDataContstants.StandartItem,
                    EkycAdditionalDataContstants.OcrFailedItemForRetry

                };



            }

        }

        if (ocrStatus && ocrIsSuccess)
        {
            dataChanged.additionalData.pages = new List<EkycPageModel>
                {
                    EkycAdditionalDataContstants.StandartItem,
                    EkycAdditionalDataContstants.OcrSuccessForNfcItem,
                    EkycAdditionalDataContstants.NfcActivePassiveAuth
                };
        }
        dataChanged.additionalData.exitTransition = "amorphie-ekyc-exit";
        // dynamic variables = new ExpandoObject();
        variables.Add("Init", true);
        variables.Add("OcrStatus", ocrStatus);

        variables.Add("CurrentOcrFailedCount", ocrCurrentFailedCount);
        variables.Add("HasNfc", hasNfc);

        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);

        return Results.Ok(variables);

    }
}
