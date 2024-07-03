using System.Dynamic;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token;

public static class RememberPasswordSms
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> SendTempPassworSms(
        [FromBody] dynamic body,
         [FromServices] IEkycService ekycService
    ){

        var transitionName = body.GetProperty("LastTransition").ToString();
        var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");
        dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());

        

        dynamic variables = new Dictionary<string, dynamic>();
        dataChanged.additionalData = new ExpandoObject();

        // Generate a temp password and send it the user via sms

        return  Results.Ok(variables);
    }
}
