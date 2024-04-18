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
        [FromServices] IEkycProvider ekycProvider
    )
    {
        var transitionName = body.GetProperty("LastTransition").ToString();

        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());

        dynamic targetObject = new System.Dynamic.ExpandoObject();
        targetObject.Data = dataChanged;

        // await ekycProvider.TestEnqura();
        // Enqura -> register 
        // Password Reset and Sim block settings configuration

        dynamic variables = new Dictionary<string, dynamic>();
        variables.Add("Init", true);

        dataChanged.additionalData = new ExpandoObject();
        dataChanged.additionalData.isEkyc = true;// gitmek istediği data 
        dataChanged.additionalData.callType = "IBSifre_ON";
        dataChanged.additionalData.customerName = "Test Customer Name"; // bu kısımları doldur.
        dataChanged.additionalData.customerSurname = "Test Customer Surname";


        targetObject.Data = dataChanged;
        targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
        targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
        variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);
        return Results.Ok(variables);

        // return Task.FromResult(Results.Ok("data"));
    }
}
