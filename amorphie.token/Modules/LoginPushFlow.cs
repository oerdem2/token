using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules
{
    public static class LoginPushFlow
    {
        public static void MapLoginPushFlowControlEndpoints(this WebApplication app)
        {
            app.MapPost("/amorphie-token-login-push-flow", loginPushFlow)
            .Produces(StatusCodes.Status200OK);

            static async Task<IResult> loginPushFlow(
            [FromBody] dynamic body,
            [FromServices] IUserService userService,
            IConfiguration configuration,
            DaprClient daprClient
            )
            {
                
                var transactionId = body.GetProperty("InstanceId").ToString();
            
                var userInfoSerialized = body.GetProperty("userSerialized").ToString();
                
                LoginResponse userInfo = JsonSerializer.Deserialize<LoginResponse>(userInfoSerialized,new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var rand = new Random();
                var code = String.Empty;

                for (int i = 0; i < 6; i++)
                {
                    code += rand.Next(10);
                }

                await daprClient.SaveStateAsync(configuration["DAPR_STATE_STORE_NAME"],$"{transactionId}_Login_Otp_Code",code);

                var pushRequest = new{
                    Sender="AutoDetect",
                    CitizenshipNo=userInfo.Reference,
                    Template=configuration["PushOtpTemplate"],
                    TemplateParams= JsonSerializer.Serialize(new{test=$"{code} şifresi ile giriş yapabilirsiniz."}),
                    SaveInbox=false,
                    Tags=new string[]{"Login Otp Flow"},
                    Process = new{
                        Name="Token Login Flow",
                        Identity="Push Login"
                    }
                };

                StringContent request = new(JsonSerializer.Serialize(pushRequest),Encoding.UTF8,"application/json");

                using var httpClient = new HttpClient();
                var httpResponse = await httpClient.PostAsync(configuration["MessagingGatewayProdUri"],request);
                
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
                    variables.message = "Push Service Error";
                    variables.LastTransition = "token-error";
                    return Results.Ok(variables);
                }
            }
        }
    }
}