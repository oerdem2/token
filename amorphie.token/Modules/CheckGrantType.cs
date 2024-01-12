
using System.Dynamic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules;

public static class CheckGrantTypes
{
    public static void MapCheckGrantTypesControlEndpoints(this WebApplication app)
    {
        app.MapPost("/amorphie-token-check-grant-type", checkGrantTypes)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> checkGrantTypes(
        [FromBody] dynamic body,
        [FromServices] IAuthorizationService authorizationService
        )
        {
            await Task.CompletedTask;
            var transitionName = body.GetProperty("LastTransition").ToString();

            var requestBodySerialized = body.GetProperty($"TRX-{transitionName}").GetProperty("Data").GetProperty("entityData").ToString();


            TokenRequest requestBody = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var clientInfoSerialized = body.GetProperty("clientSerialized").ToString();

            ClientResponse clientInfo = JsonSerializer.Deserialize<ClientResponse>(clientInfoSerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (clientInfo.allowedgranttypes == null || !clientInfo.allowedgranttypes.Any(g => g.GrantType == requestBody.GrantType))
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Client Has No Authorize To Use Requested Grant Type";
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
