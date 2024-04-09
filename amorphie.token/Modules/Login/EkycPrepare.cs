using amorphie.core.Base;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
namespace amorphie.token;

public static class EkycPrepare
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> Prepare(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibDatabaseContext        
    ){
        var transitionName = body.GetProperty("LastTransition").ToString();

            var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

            dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());

            dynamic targetObject = new System.Dynamic.ExpandoObject();

            targetObject.Data = dataChanged;
        dynamic variables = new ExpandoObject();
        variables.Init = true;

        dataChanged.additionalData = new ExpandoObject();
                dataChanged.additionalData.securityQuestions = "securityQuestions";// gitmek istediği data 
                targetObject.Data = dataChanged;
                targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
                targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
                variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);
        return Results.Ok(variables);

        // return Task.FromResult(Results.Ok("data"));
    }
}
