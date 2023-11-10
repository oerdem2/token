
using System.Dynamic;
using System.Text;
using System.Text.Json;
using amorphie.token.Services.Consent;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Profile;
using amorphie.token.Services.Transaction;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.OpenBankingFlows;

public static class SecondFactorProcess
{
    public static void MapCheckOtpControlEndpoints(this WebApplication app)
    {
        app.MapPost("/private/SecondFactorProcess", SecondFactorProcess)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> SecondFactorProcess(
        [FromBody] Guid transactionId,
        [FromServices] ITransactionService transactionService,
        [FromServices] IFlowHandler flowHandler,
        IConfiguration configuration,
        HttpRequest httpRequest
        )
        {
            await transactionService.GetTransaction(transactionId);
            var otpFlowResult = await flowHandler.StartOtpFlow(transactionService.Transaction);

            if(otpFlowResult.StatusCode != 200)
            {
                return Results.Ok(ZeebeMessageHelper.createDynamicVariable(false,"An Error Occured At 2FA","openbanking-login-error"));
            }

            return Results.Ok(ZeebeMessageHelper.createDynamicVariable(true));
        }

    }
}
