
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.TransactionHandler;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.OpenBankingFlows;

public static class TokenLoginCheckSecondFactor
{
    public static void MapTokenLoginCheckSecondFactor(this WebApplication app)
    {
        app.MapPost("/openbanking-check-otp", TokenLoginCheckSecondFactor)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> TokenLoginCheckSecondFactor(
        [FromBody] dynamic body,
        [FromServices] ITransactionService transactionService,
        [FromServices] IFlowHandler flowHandler
        )
        {
            await transactionService.GetTransaction(Guid.Parse(body.GetProperty("transactionId").ToString()));

            var otpResult = await flowHandler.CheckOtp(body.GetProperty("otpValue").ToString());
            if(otpResult.StatusCode == 403)
            {
                var transaction = transactionService.Transaction;
                transaction.OtpErrorCount++;
                transaction.TransactionState = TransactionState.OtpMissMatch;
                await transactionService.SaveTransaction(transaction);
                return Results.Ok(new{status=false,otpErrorCount = transaction.OtpErrorCount});
            }
            if(otpResult.StatusCode == 200)
            {
                return Results.Ok(new{status=true});
            }

            return Results.Ok(new{status=false});
        }

    }
}
