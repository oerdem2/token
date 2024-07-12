using System.Dynamic;
using amorphie.token.core;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public static class EkycExit
{
  [ApiExplorerSettings(IgnoreApi = true)]
  public static async Task<IResult> Exit([FromBody] dynamic body,
    [FromServices] IEkycService ekycService)
  {
    var transitionName = body.GetProperty("LastTransition").ToString();
    var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");
    dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());
    var callType = body.GetProperty("CallType").ToString();


    dynamic targetObject = new System.Dynamic.ExpandoObject();
    targetObject.Data = dataChanged;

    var fromVideoCall = dataChanged.entityData.IsFromVideoCall;

    

    
    // Add additional data object here !
    dataChanged.additionalData = new ExpandoObject();

    dataChanged.additionalData.isEkyc = true;
    dataChanged.additionalData.callType = callType;

    var ekycResult = EkycResultConstants.SelfServiceExit;
    if (fromVideoCall)
    {
      ekycResult = EkycResultConstants.VideoCallExit;
    }


    dynamic variables = new Dictionary<string, dynamic>();
    // variables here !
    variables.Add("EkycResult", ekycResult);
    variables.Add("EkycButton", "None");
    variables.Add("EkycCallType", callType);

    targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
    targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
    variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);

    return Results.Ok(variables);
  }
}
