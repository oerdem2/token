
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

public static class OpenbankingCheckUser
{
    public static void MapTokenLoginCheckUser(this WebApplication app)
    {
        app.MapPost("/openbanking-check-user", TokenLoginCheckUser)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> TokenLoginCheckUser(
        [FromBody] OpenBankingLogin body,
        [FromServices] ITransactionService transactionService
        )
        {
            await transactionService.GetTransaction(Guid.Parse(body.transactionId));
            var validateUserResult = await transactionService.CheckLoginFromWorkflow(body.username, body.password);
            if (validateUserResult.StatusCode != 200)
            {
                return Results.Ok(new { status = false });
            }

            return Results.Ok(new { status = true });
        }

    }
}
