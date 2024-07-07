
using System.Dynamic;
using System.Text;
using System.Text.Json;
using amorphie.token.core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.OtpProcess;

public static class CheckOtpFlow
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public static async Task<IResult> checkOtpFlow(
    [FromBody] dynamic body,
    IConfiguration configuration,
    DaprClient daprClient
    )
    {
        var langCode = ErrorHelper.GetLangCode(body);
        var transactionId = body.GetProperty("InstanceId").ToString();
        var transitionName = body.GetProperty("LastTransition").ToString();
        var entityData = body.GetProperty("TRX" + transitionName.ToString().Replace("-","")).GetProperty("Data").GetProperty("entityData");

        var providedCode = entityData.GetProperty("otpValue").ToString();
        var generatedCode = await daprClient.GetStateAsync<string?>(configuration["DAPR_STATE_STORE_NAME"], $"{transactionId}_ThirdFactor_Otp_Code");

        dynamic variables = new ExpandoObject();
        if (generatedCode == null)
        {
            variables.OtpProcessTimeout = true;
            variables.message = ErrorHelper.GetErrorMessage(LoginErrors.OtpTimeout, langCode);
            return Results.Ok(variables);
        }
        variables.OtpProcessTimeout = false;

        if (generatedCode != null && providedCode == generatedCode)
        {
            variables.OtpMatch = true;

            return Results.Ok(variables);
        }
        else
        {
            var OtpProcessTryCount = Convert.ToInt32(body.GetProperty("OtpProcessTryCount").ToString());
            variables.OtpMatch = false;
            variables.OtpProcessTryCount = OtpProcessTryCount++;
            variables.message = ErrorHelper.GetErrorMessage(LoginErrors.WrongOtp, langCode);
            return Results.Ok(variables);
        }
    }


}
