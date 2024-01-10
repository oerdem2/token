using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.token.core.Extensions;
using amorphie.token.core.Models.Profile;
using amorphie.token.data;
using amorphie.token.Services.TransactionHandler;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.Login
{
    public static class GenerateTokens
    {
        public static async Task<IResult> generateTokens(
        [FromBody] dynamic body,
        [FromServices] ITokenService tokenService,
        [FromServices] ITransactionService transactionService
        )
        {
            Console.WriteLine("GenerateTokens called");
            var transitionName = body.GetProperty("LastTransition").ToString();

            var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

            dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());

            dynamic targetObject = new System.Dynamic.ExpandoObject();

            targetObject.Data = dataChanged;

            var requestBodySerialized = body.GetProperty("TRXamorphiemobilelogin").GetProperty("Data").GetProperty("entityData").ToString();

            TokenRequest requestBody = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var clientInfoSerialized = body.GetProperty("clientSerialized").ToString();

            ClientResponse clientInfo = JsonSerializer.Deserialize<ClientResponse>(clientInfoSerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var userInfoSerialized = body.GetProperty("userSerialized").ToString();

            LoginResponse userInfo = JsonSerializer.Deserialize<LoginResponse>(userInfoSerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var profileSerialized = body.GetProperty("userInfoSerialized").ToString();

            SimpleProfileResponse profile = JsonSerializer.Deserialize<SimpleProfileResponse>(profileSerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            string xforwardedfor = body.GetProperty("Headers").GetProperty("xforwardedfor").ToString();
            var ipAddress = xforwardedfor.Split(",")[0].Trim();

            transactionService.IpAddress = ipAddress;
            ServiceResponse<TokenResponse> result = await tokenService.GenerateTokenWithPasswordFromWorkflow(requestBody.MapTo<GenerateTokenRequest>(), clientInfo, userInfo, profile);

            if (result.StatusCode == 200)
            {
                dataChanged.additionalData = result.Response;
                targetObject.Data = dataChanged;
                targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
                targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
                dynamic variables = new Dictionary<string, dynamic>();
                variables.Add("status", true);
                variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);
                Console.WriteLine("GenerateTokens Success");
                return Results.Ok(variables);
            }
            else
            {
                dynamic variables = new ExpandoObject();
                variables.status = false;
                variables.tokenResponse = result.Detail;
                Console.WriteLine("GenerateTokens Error " + JsonSerializer.Serialize(variables));
                return Results.Ok(variables);
            }

        }
    }
}