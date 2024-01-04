
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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
            Console.WriteLine("CheckUser called");
            var transitionName = body.GetProperty("LastTransition").ToString();

            var requestBodySerialized = body.GetProperty($"TRX-{transitionName}").GetProperty("Data").GetProperty("entityData").ToString();

            TokenRequest requestBody = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var userResponse = await userService.Login(new LoginRequest() { Reference = requestBody.Username!, Password = requestBody.Password! });

            if (userResponse.StatusCode != 200)
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = userResponse.Detail;
                variables.LastTransition = "token-error";
                Console.WriteLine("CheckUser Error " + JsonSerializer.Serialize(variables));
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = true;
                variables.userSerialized = userResponse.Response;
                Console.WriteLine("CheckUser Success");
                return Results.Ok(variables);
            }

        }

    }
}
