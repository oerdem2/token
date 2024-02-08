using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.token.data;
using amorphie.token.Services.TransactionHandler;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.Login
{
    public static class CheckMobileClient
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> checkMobileClient(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService,
        DatabaseContext databaseContext,
        ITransactionService transactionService
        )
        {
            var instanceId  = Guid.Parse(body.GetProperty("InstanceId").ToString());
            var transitionName = body.GetProperty("LastTransition").ToString();
            var requestBodySerialized = body.GetProperty("TRX-"+transitionName).GetProperty("Data").GetProperty("entityData").ToString();
            TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);

            transactionService.Logon.LogonStatus = LogonStatus.Active;
            transactionService.Logon.LogonType = !string.IsNullOrWhiteSpace(requestBodySerialized.password) ? LogonType.Password : LogonType.Phone;
            transactionService.Logon.Reference = requestBodySerialized.reference;
            
            dynamic variables = new ExpandoObject();
            variables.requestBody = requestBodySerialized;

            ServiceResponse<ClientResponse> clientResult;
            if (Guid.TryParse(request.ClientId, out Guid _))
            {
                clientResult = await clientService.ValidateClient(request.ClientId!, request.ClientSecret!);
            }
            else
            {
                clientResult = await clientService.ValidateClientByCode(request.ClientId!, request.ClientSecret!);
            }

            if (clientResult.StatusCode == 200)
            {
                variables.status = true;
                variables.clientSerialized = clientResult.Response;
            }
            else
            {
                variables.status = false;
                variables.message = clientResult.Detail;
                variables.LastTransition = "amorphie-login-error";
            }

            return Results.Ok(variables);
        }
    }
}