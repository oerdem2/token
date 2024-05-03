using amorphie.core.Base;
using amorphie.token.core;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using System.Text.Json;
namespace amorphie.token;

public static class EkycPrepare
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> Prepare(
        [FromBody] dynamic body,
        [FromServices] IEkycService ekycService
    )
    {
        var transitionName = body.GetProperty("LastTransition").ToString();
        var transactionId = body.GetProperty("InstanceId").ToString();
        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());
        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());

        dynamic targetObject = new System.Dynamic.ExpandoObject();
        targetObject.Data = dataChanged;
        var reference = dataChanged.entityData.UserName;
        var instance = Guid.NewGuid();
        Guid.TryParse(transactionId, out instance);
        //Register the enqura 
        var registerResult = await ekycService.CreateSession(instance, reference, core.EkycProcess.ProcessType.IbPasswordRenew);


        dynamic variables = new Dictionary<string, dynamic>();
        if (registerResult.IsSuccessful)
        {

            variables.Add("ReferenceId", registerResult.ReferenceId);

            // Get Session from integration 

            
        }



        // Set config variables :) 
        variables.Add("Init", true);
        variables.Add("CurrentOcrFailedCount", 0);
        variables.Add("CurrentNfcFailedCount", 0);
        variables.Add("CurrentFaceFailedCount", 0);

        variables.Add("OcrFailedTryCount", EkycConstants.OcrFailedTryCount);
        variables.Add("NfcFailedTryCount", EkycConstants.NfcFailedTryCount);
        variables.Add("FaceFailedTryCount", EkycConstants.FaceFailedTryCount);

        variables.Add("OcrFailedMaxTryCount", EkycConstants.OcrFailedMaxTryCount);
        variables.Add("NfcFailedMaxTryCount", EkycConstants.NfcFailedMaxTryCount);
        variables.Add("FaceFailedMaxTryCount", EkycConstants.FaceFailedMaxTryCount);


        variables.Add("UserName", reference);


        dataChanged.additionalData = new ExpandoObject();
        dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
        dataChanged.additionalData.callType = registerResult.CallType;
        dataChanged.additionalData.customerName = registerResult.Name; // bu kısımları doldur.
        dataChanged.additionalData.customerSurname = registerResult.Surname;


        targetObject.Data = dataChanged;
        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);
        targetObject.Data = dataChanged;
        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);
        return Results.Ok(variables);

    }
}
