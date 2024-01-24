using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.token.data;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.Login
{
    public static class CheckScopes
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> checkScopes(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ibDatabaseContext
        )
        {
            await Task.CompletedTask;
            var requestBodySerialized = body.GetProperty($"TRXamorphiemobilelogin").GetProperty("Data").GetProperty("entityData").ToString();
            TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);

            var clientInfoSerialized = body.GetProperty("clientSerialized").ToString();
            ClientResponse client = JsonSerializer.Deserialize<ClientResponse>(clientInfoSerialized);

            if (!request.Scopes!.ToList().All(client.allowedscopetags!.Contains))
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Client is Not Authorized For Requested Scopes";
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = true;
                return Results.Ok(variables);
            }
        }
    }
}