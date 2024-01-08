
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules;

public static class CheckOtpFlow
{
    public static async Task<IResult> checkOtpFlow(
    [FromBody] dynamic body,
    [FromServices] IAuthorizationService authorizationService,
    [FromServices] IUserService userService,
    IConfiguration configuration,
    DaprClient daprClient
    )
    {
        var transactionId = body.GetProperty("InstanceId").ToString();
        var entityData = body.GetProperty("TRXamorphiemobileloginsendotp").GetProperty("Data").GetProperty("entityData").ToString();

        var entityObj = JsonSerializer.Deserialize<Dictionary<string, object>>(entityData);
        var providedCode = entityObj["otpValue"].ToString();
        var generatedCode = await daprClient.GetStateAsync<string?>(configuration["DAPR_STATE_STORE_NAME"], $"{transactionId}_Login_Otp_Code");

        if (generatedCode != null && providedCode == generatedCode)
        {
            dynamic variables = new ExpandoObject();
            variables.otpMatch = true;
            
            //await userService.SaveDevice(userInfo.Id,Guid.Parse(clientInfo.id));
            return Results.Ok(variables);
        }
        else
        {
            var otpTryCount = Convert.ToInt32(body.GetProperty("OtpTryCount").ToString());
            dynamic variables = new ExpandoObject();
            variables.otpMatch = false;
            variables.OtpTryCount = otpTryCount++;
            variables.message = "Otp Check Failed";
            variables.LastTransition = "token-error";
            Console.WriteLine("CheckOtp Error " + JsonSerializer.Serialize(variables));
            return Results.Ok(variables);
        }
    }


}
