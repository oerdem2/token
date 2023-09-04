using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules
{
    public static class SetLoginType
    {
        public static void MapSetLoginTypeControlEndpoints(this WebApplication app)
        {
            app.MapPost("/amorphie-token-set-login-type", setLoginType)
            .Produces(StatusCodes.Status200OK);

            static async Task<IResult> setLoginType(
            [FromBody] dynamic body,
            [FromServices] IUserService userService,
            IConfiguration configuration,
            DaprClient daprClient
            )
            {
                var userInfoSerialized = body.GetProperty("userSerialized").ToString();

                LoginResponse userInfo = JsonSerializer.Deserialize<LoginResponse>(userInfoSerialized, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });


                var clientInfoSerialized = body.GetProperty("clientSerialized").ToString();

                ClientResponse clientInfo = JsonSerializer.Deserialize<ClientResponse>(clientInfoSerialized, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                ServiceResponse<object> response = await userService.CheckDevice(userInfo.Id, Guid.Parse(clientInfo.id));

                if (response.StatusCode == 200)
                {
                    dynamic variables = new ExpandoObject();
                    variables.status = true;
                    variables.loginFlow = "push";
                    return Results.Ok(variables);
                }
                if (response.StatusCode == 404)
                {
                    dynamic variables = new ExpandoObject();
                    variables.status = true;
                    variables.loginFlow = "otp";
                    return Results.Ok(variables);
                }
                else
                {
                    dynamic variables = new ExpandoObject();
                    variables.status = false;
                    variables.message = response.Detail;
                    variables.LastTransition = "token-error";
                    return Results.Ok(variables);
                }
            }
        }
    }
}