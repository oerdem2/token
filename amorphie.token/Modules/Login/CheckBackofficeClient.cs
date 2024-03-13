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
    public static class CheckBackofficeClient
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> checkBackofficeClient(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService,
        ITransactionService transactionService
        )
        {
            var transitionName = body.GetProperty("LastTransition").ToString();
            var requestBodySerialized = body.GetProperty("TRX-" + transitionName).GetProperty("Data").GetProperty("entityData").ToString();
            TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);

            transactionService.Logon.LogonStatus = LogonStatus.Active;
            transactionService.Logon.LogonType = !string.IsNullOrWhiteSpace(request.Password) ? LogonType.Password : LogonType.Phone;
            transactionService.Logon.Reference = request.Username;

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
                transactionService.Logon.ClientId = clientResult.Response.id;
            }
            else
            {
                variables.status = false;
                variables.message = clientResult.Detail;
            }

            return Results.Ok(variables);
        }
    }
}