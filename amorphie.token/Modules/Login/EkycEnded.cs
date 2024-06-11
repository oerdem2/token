using System.Dynamic;
using amorphie.token.core;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public static class EkycEnded
{
     [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> End([FromBody] dynamic body,
      [FromServices] IEkycService ekycService)
    {
        var transitionName = body.GetProperty("LastTransition").ToString();
        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");
        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());


        dynamic targetObject = new System.Dynamic.ExpandoObject();
        targetObject.Data = dataChanged;
        // Add additional data object here !
        dataChanged.additionalData = new ExpandoObject();
        var callType = body.GetProperty("CallType").ToString();
        var instance = body.GetProperty("Instance").ToString();
        // var name = body.GetProperty("Name").ToString();
        // var surname = body.GetProperty("Surname").ToString();
        
        dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
        dataChanged.additionalData.callType = callType;
        // dataChanged.additionalData.customerName = name; // bu kısımları doldur.
        // dataChanged.additionalData.customerSurname = surname;
        var ApplicantFullName = body.GetProperty("ApplicantFullName").ToString();
        dataChanged.additionalData.applicantFullName = ApplicantFullName;
        dataChanged.additionalData.instanceId = instance;

        dataChanged.additionalData.pages = new List<EkycPageModel>
                {
                    EkycAdditionalDataContstants.StandartItem
                    
                }; 
        

        dynamic variables = new Dictionary<string, dynamic>();
        // variables here !



        

        // variables.Add("EkycResult",EkycResultConstants.VideoCallCompleted);
        variables.Add("EkycCallType",callType);
        
        
        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);

        return Results.Ok(variables);
    }
}
