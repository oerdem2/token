
using System.Dynamic;
using System.Text;
using System.Text.Json;
using amorphie.token.Services.Consent;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Profile;
using amorphie.token.Services.Transaction;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.OpenBankingFlows;

public static class ValidateUser
{
    public static void MapCheckOtpControlEndpoints(this WebApplication app)
    {
        app.MapPost("/private/ValidateUser", ValidateUser)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> ValidateUser(
        [FromBody] dynamic body,
        [FromServices] ITransactionService transactionService,
        HttpRequest httpRequest
        )
        {
            var openBankingLoginRequest = JsonSerializer.Deserialize<OpenBankingLogin>(body);
            await transactionService.GetTransaction(Guid.Parse(openBankingLoginRequest.transactionId));

            var validateUser = await transactionService.CheckLoginFromWorkflow(openBankingLoginRequest.username,openBankingLoginRequest.password);
            if(validateUser.StatusCode != 200)
            {
                return Results.Ok(ZeebeMessageHelper.createDynamicVariable(false,"An Error Occured At LoginServices","openbanking-login-error"));
            }

            return Results.Ok(ZeebeMessageHelper.createDynamicVariable(true));
        }

    }
}
