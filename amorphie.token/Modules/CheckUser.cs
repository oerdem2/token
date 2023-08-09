
using System.Text.Json;
using amorphie.token.core.Models.Token;
using amorphie.token.core.Models;
using amorphie.token.Services.Authorization;
using Microsoft.AspNetCore.Mvc;
using amorphie.token.core.Helpers;
using System.Dynamic;

namespace amorphie.token.Modules;

public static class CheckUser
{
    public static void MapCheckUserControlEndpoints(this WebApplication app)
    {
        app.MapPost("/amorphie-token-check-user", checkUser)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> checkUser(
        [FromBody] dynamic body,
        [FromServices] IUserService userService
        )
        {
            var transitionName = body.GetProperty("LastTransition").ToString();

            var requestBodySerialized = body.GetProperty($"TRX-{transitionName}").GetProperty("Data").GetProperty("entityData").ToString();
            
            TokenRequest requestBody = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized,new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var userResponse = await userService.Login(new LoginRequest(){Reference = requestBody.username,Password = requestBody.password});
            
            if(userResponse.StatusCode != 200)
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = userResponse.Detail;
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = true;
                variables.userSerialized = userResponse.Response;
                return Results.Ok(variables);
            }

        }

    }
}
