
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.TransactionHandler;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.OpenBankingFlows;

public static class TokenLoginSendOtp
{
    public static void MapTokenLoginSendOtp(this WebApplication app)
    {
        app.MapPost("/openbanking-send-otp", TokenLoginSendOtp)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> TokenLoginSendOtp(
        [FromBody] dynamic body,
        [FromServices] ITransactionService transactionService,
        [FromServices] IFlowHandler flowHandler
        )
        {
            await transactionService.GetTransaction(Guid.Parse(body.GetProperty("transactionId").ToString()));
            var transaction = transactionService.Transaction;
            
            var otpResult = await flowHandler.StartOtpFlow(transaction);
            if(otpResult.StatusCode != 200)
            {
                return Results.Ok(new{status=false});
            }

            return Results.Ok(new{status=true,otpErrorCount = transaction.OtpErrorCount});
        }

    }
}
