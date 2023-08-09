
using System.Text.Json;
using amorphie.token.core.Models.Token;
using amorphie.token.core.Models;
using amorphie.token.Services.Authorization;
using Microsoft.AspNetCore.Mvc;
using amorphie.token.core.Helpers;
using System.Dynamic;

namespace amorphie.token.Modules;

public static class CheckScopes
{
    public static void MapCheckScopesControlEndpoints(this WebApplication app)
    {
        app.MapPost("/amorphie-token-check-scopes", checkScopes)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> checkScopes(
        [FromBody] dynamic body,
        [FromServices] IAuthorizationService authorizationService
        )
        {
            var transitionName = body.GetProperty("LastTransition").ToString();

            var requestBodySerialized = body.GetProperty($"TRX-{transitionName}").GetProperty("Data").GetProperty("entityData").ToString();
            
            TokenRequest requestBody = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized,new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var clientInfoSerialized = body.GetProperty("clientSerialized").ToString();
            
            ClientResponse clientInfo = JsonSerializer.Deserialize<ClientResponse>(clientInfoSerialized,new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            var requestedScopes = requestBody.scopes.ToList();

            if(!requestedScopes.All(clientInfo.allowedscopetags.Contains))
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Client is Not Authorized For Requested Scopes";
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
