
using System.Dynamic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules;

public static class Login
{
    public static void MapLoginControlEndpoints(this WebApplication app)
    {
        app.MapPost("/login2", login)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> login(
        [FromBody] TokenRequest body
        )
        {
            return Results.Accepted();
        }

    }
}
