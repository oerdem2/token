
using System.Text.Json;
using amorphie.token.core.Models.Token;
using amorphie.token.core.Models;
using amorphie.token.Services.Authorization;
using Microsoft.AspNetCore.Mvc;
using amorphie.token.core.Helpers;
using System.Dynamic;

namespace amorphie.token.Modules;

public static class GenerateTokens
{
    public static void MapGenerateTokensControlEndpoints(this WebApplication app)
    {
        app.MapPost("/amorphie-token-generate-tokens", generateTokens)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> generateTokens(
        [FromBody] dynamic body,
        [FromServices] IAuthorizationService authorizationService
        )
        {
            var transitionName = body.GetProperty("LastTransition").ToString();
            dynamic bodyData = body.GetProperty("TRX-start-password-flow").GetProperty("Data");

            var requestBodySerialized = body.GetProperty("TRX-start-password-flow").GetProperty("Data").GetProperty("entityData").ToString();
            
            TokenRequest requestBody = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized,new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var clientInfoSerialized = body.GetProperty("clientSerialized").ToString();
            
            ClientResponse clientInfo = JsonSerializer.Deserialize<ClientResponse>(clientInfoSerialized,new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var userInfoSerialized = body.GetProperty("userSerialized").ToString();
            
            LoginResponse userInfo = JsonSerializer.Deserialize<LoginResponse>(userInfoSerialized,new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ServiceResponse<TokenResponse> result = await authorizationService.GenerateTokenWithPasswordFromWorkflow(requestBody,clientInfo,userInfo);

            if(result.StatusCode == 200)
            {
                bodyData.additionalData = result.Response;
                dynamic variables = new Dictionary<string,dynamic>();
                variables.Add("status" ,true);
                variables.Add($"TRX-{transitionName}",bodyData);
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.tokenResponse = result.Detail;
                variables.LastTransition = "token-error";
                return Results.Ok(variables);
            }

        }

    }
}
