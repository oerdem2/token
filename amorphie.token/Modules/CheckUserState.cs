
using System.Text.Json;
using amorphie.token.core.Models.Token;
using amorphie.token.core.Models;
using amorphie.token.Services.Authorization;
using Microsoft.AspNetCore.Mvc;
using amorphie.token.core.Helpers;
using System.Dynamic;

namespace amorphie.token.Modules;

public static class CheckUserState
{
    public static void MapCheckUserStateControlEndpoints(this WebApplication app)
    {
        app.MapPost("/amorphie-token-check-user-state", checkUserState)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> checkUserState(
        [FromBody] dynamic body,
        [FromServices] IAuthorizationService authorizationService
        )
        {

            var userInfoSerialized = body.GetProperty("userSerialized").ToString();

            LoginResponse userInfo = JsonSerializer.Deserialize<LoginResponse>(userInfoSerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });


            if ((userInfo?.State.ToLower() != "active" && userInfo?.State.ToLower() != "new"))
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "User is disabled";
                variables.LastTransition = "token-error";
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = true;
                return Results.Ok(variables);
            }

        }

    }
}
