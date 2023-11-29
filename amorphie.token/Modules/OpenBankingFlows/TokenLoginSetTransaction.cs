

using amorphie.token.Services.TransactionHandler;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.OpenBankingFlows;

public static class TokenLoginSetTransaction
{
    public static void MapTokenLoginSetTransaction(this WebApplication app)
    {
        app.MapPost("/openbanking-set-transaction", TokenLoginSetTransaction)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> TokenLoginSetTransaction(
        [FromBody] dynamic body,
        [FromServices] ITransactionService transactionService,
        HttpRequest request
        )
        {
            await transactionService.GetTransaction(Guid.Parse(body.GetProperty("transactionId").ToString()));
            var transaction = transactionService.Transaction;

            var state = request.Headers.FirstOrDefault(h=> h.Key.Equals("State")).Value.ToString();            
            if(state.Equals("completed"))
            {
                transaction.TransactionState = TransactionState.Completed;
            }
            if(state.Equals("error"))
            {
                transaction.TransactionState = TransactionState.Error;
            }
            if(state.Equals("next"))
            {
                transaction.Next = true;
            }

            await transactionService.SaveTransaction(transaction);
            return Results.Ok(new{status=true});
        }

    }
}
