using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.Login
{
    public static class CheckMobileClient
    {
        public static async Task<IResult> checkMobileClient(
        [FromBody] dynamic body,
        [FromServices] IClientService clientService
        )
        {
            var requestBodySerialized = body.GetProperty($"TRXamorphiemobilelogin").GetProperty("Data").GetProperty("entityData").ToString();
            TokenRequest request = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized);

            var clientResult = await clientService.ValidateClient(request.ClientId!,request.ClientSecret!);
            if(clientResult.StatusCode == 200)
            {
                dynamic variables = new ExpandoObject();
                variables.status = true;
                variables.clientSerialized = clientResult.Response;
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.message = clientResult.Detail;
                variables.LastTransition = "amorphie-login-error";
                return Results.Ok(variables);
            }
        }
    }
}