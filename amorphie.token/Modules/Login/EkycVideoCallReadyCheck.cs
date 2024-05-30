using System.Dynamic;
using amorphie.token.core;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public class EkycVideoCallReadyCheck
{
     public static async Task<IResult> Check(
       [FromBody] dynamic body,
       [FromServices] IEkycService ekycService,
       IConfiguration configuration
   )
    {

        var transitionName = body.GetProperty("LastTransition").ToString();

        // var transactionId = body.GetProperty("InstanceId").ToString();
        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");
        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());
        dynamic targetObject = new System.Dynamic.ExpandoObject();
        targetObject.Data = dataChanged;
        dynamic variables = new Dictionary<string, dynamic>();
        dataChanged.additionalData = new ExpandoObject();


        var isSuccess = dataChanged.entityData.IsSuccess;
        var callType = body.GetProperty("CallType").ToString();
        var instance = body.GetProperty("Instance").ToString();
        var name = body.GetProperty("Name").ToString();
        var surname = body.GetProperty("Surname").ToString();
        dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
        dataChanged.additionalData.callType = callType;
        dataChanged.additionalData.customerName = name; // bu kısımları doldur.
        dataChanged.additionalData.customerSurname = surname;
        dataChanged.additionalData.instanceId = instance;

        


        if(!isSuccess){
            dataChanged.additionalData.pages = new List<EkycPageModel>
                {
                    EkycAdditionalDataContstants.StandartItem,
                    EkycAdditionalDataContstants.VideoCallReadyFail
                    
                };
        }else{
             dataChanged.additionalData.pages = new List<EkycPageModel>
                {
                    EkycAdditionalDataContstants.StandartItem,
                    EkycAdditionalDataContstants.VideoCallReadySuccessWait,
                    EkycAdditionalDataContstants.VideoCallReadySuccessOcrRetryFail,
                    EkycAdditionalDataContstants.VideoCallReadySuccessNfcRetryFail,
                    EkycAdditionalDataContstants.VideoCallReadySuccessFaceRetryFail
                    
                };
        }

        
        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);
        return Results.Ok(variables);

    }
}
