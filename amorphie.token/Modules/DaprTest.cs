
using System.Text.Json;
using amorphie.token.core.Models.Token;
using amorphie.token.core.Models;
using amorphie.token.Services.Authorization;
using Microsoft.AspNetCore.Mvc;
using amorphie.token.core.Helpers;
using System.Dynamic;

namespace amorphie.token.Modules;

public static class DaprTest
{
    public static void MapDaprTestControlEndpoints(this WebApplication app)
    {
        app.MapPost("/start-workflow", startWorkflow)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> startWorkflow(
        [FromServices] DaprClient daprClient,
        [FromBody] dynamic body
        )
        {
            dynamic messageData = new ExpandoObject();

            dynamic data = new ExpandoObject();
            data.transactionId = "12312512512616161";

            dynamic client = new ExpandoObject();
            client.Id = "1231312";
            client.Secret = "2151251251";
            data.Client = client;

            data.body = body;

            messageData.messageName  = "start-password-flow";
            messageData.variables = data;
            await daprClient.InvokeBindingAsync<dynamic,dynamic>("zeebe-local","publish-message",messageData);
            return Results.Ok();
        }

    }
}
