
using System.Dynamic;
using System.Text;
using System.Text.Json;
using amorphie.token.Services.Consent;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.Profile;
using amorphie.token.Services.Transaction;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.OpenBankingFlows;

public static class SetCustomerType
{
    public static void MapCheckOtpControlEndpoints(this WebApplication app)
    {
        app.MapPost("/private/SetCustomerType", SetCustomerType)
        .Produces(StatusCodes.Status200OK);

        static async Task<IResult> SetCustomerType(
        [FromBody] dynamic body,
        [FromServices] IProfileService profileService,
        [FromServices] IConsentService consentService,
        [FromServices] ITransactionService transactionService,
        IConfiguration configuration,
        DaprClient daprClient,
        HttpRequest httpRequest
        )
        {
            var openBankingLoginRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(body);
            
            var consentResult = await consentService.GetConsent(openBankingLoginRequest.riza_no);
            if(consentResult.StatusCode != 200)
            {
                return Results.Ok(ZeebeMessageHelper.createDynamicVariable(false,"Consent Not Found","openbanking-login-error"));
            }
            var consent = consentResult.Response;

            var consentData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(consent.additionalData);
            var kmlkNo = consentData.kmlk.kmlkVrs.ToString();

            var customerInfoResult = await profileService.GetCustomerProfile(kmlkNo);
            if(customerInfoResult.StatusCode != 200)
            {
                return Results.Ok(ZeebeMessageHelper.createDynamicVariable(false,"Couldn't Get Customer Info","openbanking-login-error"));
            }
            var customerInfo = customerInfoResult.Response;
            
            var transaction = new core.Models.Transaction.Transaction()
            {
                Id = openBankingLoginRequest.transaction_id,
                ConsentId = openBankingLoginRequest.consent_id,
                Profile = customerInfo
            };
            
            await transactionService.SaveTransaction(transaction);

            return Results.Ok(new{
                status = true
            });
        }

    }
}
