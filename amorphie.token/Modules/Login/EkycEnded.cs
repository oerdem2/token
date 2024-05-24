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

        
        

        dynamic variables = new Dictionary<string, dynamic>();
        // variables here !
        

        variables.Add("Result",EkycResultConstants.VideoCallCompleted);

        targetObject.Data = dataChanged;
        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);

        return Results.Ok(variables);
    }
}
