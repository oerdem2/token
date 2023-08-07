

using Microsoft.AspNetCore.Mvc;
using System.Dynamic;

namespace amorphie.token.Modules;

public static class DaprTest
{
    public static void MapDaprTestControlEndpoints(this WebApplication app)
    {
        app.MapPost("/start-workflow", startWorkflow)
        .Produces(StatusCodes.Status200OK);

        app.MapPost("/introspect",introspect)
        .Produces(StatusCodes.Status200OK);

        app.MapGet("/secured",secured)
        .Produces(StatusCodes.Status200OK);

 

        static async Task<IResult> secured(
            HttpRequest request
        )
        {
            foreach (var header in request.Headers)
            {
                Console.WriteLine($"Introspect header {header.Key}:{header.Value} ");
            }
            return Results.Ok(new{token="valid"});
        }

 

        static async Task<IResult> introspect(
        [FromBody] dynamic data,HttpRequest request
        )
        {
            foreach (var header in request.Headers)
            {
                Console.WriteLine($"Introspect header {header.Key}:{header.Value} ");
            }
            return Results.Ok(new{active = true,scope = "test mest"});
        }

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
