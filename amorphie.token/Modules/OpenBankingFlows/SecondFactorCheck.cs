
using System.Dynamic;
using System.Text;
using System.Text.Json;
using amorphie.token.Services.Consent;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Profile;
using amorphie.token.Services.Transaction;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.OpenBankingFlows;

public static class SecondFactorCheck
{
    public static void MapCheckOtpControlEndpoints(this WebApplication app)
    {
        app.MapPost("/private/SecondFactorCheck", SecondFactorCheck)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> SecondFactorCheck(
        [FromBody] dynamic body,
        [FromServices] ITransactionService transactionService,
        [FromServices] IFlowHandler flowHandler,
        IConfiguration configuration,
        HttpRequest httpRequest
        )
        {
            
            var otpRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
            await transactionService.GetTransaction(otpRequest.transanction_id);   
            var checkSecondFactor = await flowHandler.CheckOtp(otpRequest.otpValue);

            if(checkSecondFactor.StatusCode != 200)
            {
                return Results.Ok(ZeebeMessageHelper.createDynamicVariable(false,"OTP doesn't match","openbanking-login-error"));
            }

            return Results.Ok(ZeebeMessageHelper.createDynamicVariable(true));
        }

    }
}
