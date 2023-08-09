
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules;

public static class LoginOtpFlow
{
    public static void MapLoginOtpFlowControlEndpoints(this WebApplication app)
    {
        app.MapPost("/amorphie-token-login-otp-flow", loginOtpFlow)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> loginOtpFlow(
        [FromBody] dynamic body,
        [FromServices] IAuthorizationService authorizationService,
        IConfiguration configuration,
        DaprClient daprClient
        )
        {
            var transactionId = body.GetProperty("InstanceId").ToString();
            
            var rand = new Random();
            var code = String.Empty;

            for (int i = 0; i < 6; i++)
            {
                code += rand.Next(10);
            }

            await daprClient.SaveStateAsync(configuration["DAPR_STATE_STORE_NAME"],$"{transactionId}_Login_Otp_Code",code);

            var otpRequest = new{
                Sender="AutoDetect",
                SmsType="Otp",
                Phone=new{
                    CountryCode=configuration["PhoneTestCountryCode"],
                    Prefix=configuration["PhoneTestPrefix"],
                    Number=configuration["PhoneTestNumber"]
                },
                Content = $"{code} şifresi ile giriş yapabilirsiniz",
                Process = new{
                    Name="Token Login Flow",
                    Identity="Otp Login"
                }
            };

            StringContent request = new(JsonSerializer.Serialize(otpRequest),Encoding.UTF8,"application/json");

            using var httpClient = new HttpClient();
            var httpResponse = await httpClient.PostAsync(configuration["MessagingGatewayUri"],request);
            
            if(httpResponse.IsSuccessStatusCode)
            {
                dynamic variables = new ExpandoObject();
                variables.status = true;
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Otp Service Error";
                return Results.Ok(variables);
            }
        }

    }
}
