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
    public static class CheckGrantType
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> checkGrantType(
        [FromBody] dynamic body,
        [FromServices] IbDatabaseContext ıbDatabaseContext
        )
        {
            await Task.CompletedTask;
            var requestBodySerialized = body.GetProperty($"TRXamorphiemobilelogin").GetProperty("Data").GetProperty("entityData").ToString();
            TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);

            var clientInfoSerialized = body.GetProperty("clientSerialized").ToString();
            ClientResponse client = JsonSerializer.Deserialize<ClientResponse>(clientInfoSerialized);

            if (client.allowedgranttypes == null || !client.allowedgranttypes.Any(g => g.GrantType == request.GrantType))
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = "Client Has No Authority To Use Requested Scopes";
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