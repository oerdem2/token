
using System.Dynamic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules;

public static class CheckState
{
    public static void MapSetStateControlEndpoints(this WebApplication app)
    {
        app.MapPost("/set-state", setState)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> setState(
        [FromBody] dynamic body,
        [FromServices] IAuthorizationService authorizationService
        )
        {

            dynamic variables = new ExpandoObject();
            variables.status = true;
            return Results.Ok(variables);

        }

    }
}
