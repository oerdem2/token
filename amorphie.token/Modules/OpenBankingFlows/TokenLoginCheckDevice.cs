
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
            await transactionService.GetTransaction(Guid.Parse(body.GetProperty("transactionId").ToString()));
            var transaction = transactionService.Transaction;

            var checkDeviceResult = await userService.CheckDevice(transactionService.Transaction.User!.Id, Guid.Parse("6c1a722d-5125-45fd-81c7-cdf7d0a7a10b"));
            if (checkDeviceResult.StatusCode != 200)
            {
                transaction.SecondFactorMethod = SecondFactorMethod.Otp;
                await transactionService.SaveTransaction(transaction);
                return Results.Ok(new { status = true });
            }

            transaction.SecondFactorMethod = SecondFactorMethod.Push;
            await transactionService.SaveTransaction(transaction);

            return Results.Ok(new { status = true });
        }

    }
}
