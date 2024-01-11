
using System.Dynamic;
using System.Text;
using System.Text.Json;
using amorphie.core.Base;
using amorphie.token.Services.Consent;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Profile;
using amorphie.token.Services.TransactionHandler;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.OpenBankingFlows;

public static class TokenLoginCheckDevice
{
    public static void MapTokenLoginCheckDevice(this WebApplication app)
    {
        app.MapPost("/openbanking-check-device", TokenLoginCheckDevice)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> TokenLoginCheckDevice(
        [FromBody] dynamic body,
        [FromServices] ITransactionService transactionService,
        [FromServices] IUserService userService
        )
        {
            return Results.Json(new { t = "123" });
        }

    }
}
