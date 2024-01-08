
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules;

public static class ClearOtpFlow
{
    public static async Task<IResult> clearOtpFlow(
    [FromBody] dynamic body,
    IConfiguration configuration,
    DaprClient daprClient
    )
    {
        var transactionId = body.GetProperty("InstanceId").ToString();
        
        await daprClient.DeleteStateAsync(configuration["DAPR_STATE_STORE_NAME"], $"{transactionId}_Login_Otp_Code");

        dynamic variables = new ExpandoObject();
        variables.status = true;
        
        return Results.Ok(variables);
    }


}
