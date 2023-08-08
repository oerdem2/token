
using System.Text.Json;
using amorphie.token.core.Models.Token;
using amorphie.token.core.Models;
using amorphie.token.Services.Authorization;
using Microsoft.AspNetCore.Mvc;
using amorphie.token.core.Helpers;
using amorphie.token.core.Models.Client;
using System.Dynamic;

namespace amorphie.token.Modules;

public static class ValidateClient
{
    public static void MapValidateClientControlEndpoints(this WebApplication app)
    {
        app.MapPost("/validate-client", validateClient)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> validateClient(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService
        )
        {
            var requestBodySerialized = body.GetProperty("body").ToString();
            
            var requestBody = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized,new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ServiceResponse<ClientResponse> clientResult = await clientService.ValidateClient(requestBody.client_id,requestBody.client_secret);
            
            if(clientResult.StatusCode == 200)
            {
                dynamic variables = new ExpandoObject();
                variables.status = true;
                variables.clientSerialized = clientResult.Response;
                variables.loginFlow = "Otp";
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = clientResult.Detail;
                return Results.Ok(variables);
            }
        }

    }
}
