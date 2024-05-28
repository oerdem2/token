using System.Dynamic;
using amorphie.token.core;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public static class EkycSetAdditionalData
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> Add(
    [FromBody] dynamic body,
    [FromServices] IEkycService ekycService
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

        var callType = body.GetProperty("CallType").ToString();
        var instance = body.GetProperty("Instance").ToString();
        var name = body.GetProperty("Name").ToString();
        var surname = body.GetProperty("Surname").ToString();
        dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
        dataChanged.additionalData.callType = callType;
        dataChanged.additionalData.customerName = name; // bu kısımları doldur.
        dataChanged.additionalData.customerSurname = surname;
        dataChanged.additionalData.instanceId = instance;

        var stepName = body.GetProperty("ForStepname").ToString();

        if(stepName=="connection"){
            dataChanged.additionalData.pages = new List<EkycPageModel>{
                EkycAdditionalDataContstants.StandartItem
            };
        }

        if(stepName=="ocr"){
             dataChanged.additionalData.pages = new List<EkycPageModel>{
                EkycAdditionalDataContstants.StandartItem
            };
        }

        if(stepName=="init"){
             dataChanged.additionalData.pages = new List<EkycPageModel>{
                EkycAdditionalDataContstants.StandartItem
            };
        }







        variables.Add("Init", true);


        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);
        return Results.Ok(variables);

        // return Task.FromResult(Results.Ok("data"));
    }
}
