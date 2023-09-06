
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules;

public static class CheckOtp
{
    public static void MapCheckOtpControlEndpoints(this WebApplication app)
    {
        app.MapPost("/amorphie-token-check-otp-login-flow", checkOtp)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> checkOtp(
        [FromBody] dynamic body,
        [FromServices] IAuthorizationService authorizationService,
        IConfiguration configuration,
        DaprClient daprClient
        )
        {
            Console.WriteLine("CheckOtp called");
            var transactionId = body.GetProperty("InstanceId").ToString();
            Console.WriteLine("check otp txn Id:"+transactionId);
            var entityData = body.GetProperty("TRX-send-otp-login-flow").GetProperty("Data").GetProperty("entityData").ToString();

            var entityObj = JsonSerializer.Deserialize<Dictionary<string,object>>(entityData);
            var providedCode = entityObj["otpValue"].ToString();

            var generatedCode = await daprClient.GetStateAsync<string>(configuration["DAPR_STATE_STORE_NAME"],$"{transactionId}_Login_Otp_Code");
                  
            if(providedCode == generatedCode)
            {
                dynamic variables = new ExpandoObject();
                variables.status = true;
                Console.WriteLine("CheckOtp Success");
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Otp Check Failed";
                variables.LastTransition = "token-error";
                Console.WriteLine("CheckOtp Error "+JsonSerializer.Serialize(variables));
                return Results.Ok(variables);
            }
        }

    }
}
