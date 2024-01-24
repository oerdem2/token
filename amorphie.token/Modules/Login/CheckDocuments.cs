using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.core.Base;
using amorphie.token.core.Extensions;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.core.Models.Profile;
using amorphie.token.data;
using amorphie.token.Services.Consent;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.Login
{
    public static class CheckDocuments
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public static async Task<IResult> checkDocuments(
        [FromBody] dynamic body,
        [FromServices] IConsentService consentService,
        [FromServices] ITokenService tokenService,
        [FromServices] IConfiguration configuration
        )
        {
            await Task.CompletedTask;

            var transitionName = body.GetProperty("LastTransition").ToString();

            var dataBody = body.GetProperty($"TRX-{transitionName}").GetProperty("Data");

            dynamic dataChanged = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(dataBody.ToString());

            dynamic targetObject = new System.Dynamic.ExpandoObject();

            targetObject.Data = dataChanged;

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

            var requestBodySerialized = body.GetProperty("TRXamorphiemobilelogin").GetProperty("Data").GetProperty("entityData").ToString();

            TokenRequest requestBody = JsonSerializer.Deserialize<TokenRequest>(requestBodySerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var profileSerialized = body.GetProperty("userInfoSerialized").ToString();

            SimpleProfileResponse profile = JsonSerializer.Deserialize<SimpleProfileResponse>(profileSerialized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ServiceResponse<TokenResponse> result = await tokenService.GenerateTokenWithPasswordFromWorkflow(requestBody.MapTo<GenerateTokenRequest>(), clientInfo, userInfo, profile);

            using var httpClient = new HttpClient();
            StringContent request = new(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var httpResponse = await httpClient.PostAsync(configuration["localAddress"] + "public/Token", request);
            var resp = await httpResponse.Content.ReadFromJsonAsync<TokenResponse>();
            dynamic variables = new Dictionary<string, dynamic>();

            var documentsResponse = await consentService.CheckDocument(clientInfo.id!, "7b19daa2-8793-45d2-9d96-aa7540c9d1ab", userInfo.Reference);

            var documents = documentsResponse.Response;
            if (!documents!.isAuthorized)
            {
                dataChanged.additionalData = new ExpandoObject();
                dataChanged.additionalData.documents = documents.contractDocuments;
                dataChanged.additionalData.tempToken = resp!.AccessToken;
                targetObject.Data = dataChanged;
                targetObject.TriggeredBy = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredBy").ToString());
                targetObject.TriggeredByBehalfOf = Guid.Parse(body.GetProperty($"TRX-{transitionName}").GetProperty("TriggeredByBehalfOf").ToString());
                variables.Add($"TRX{transitionName.ToString().Replace("-", "")}", targetObject);
                variables.Add("documentsToApprove", true);
            }
            else
            {
                variables.Add("documentsToApprove", false);
            }

            return Results.Ok(variables);
        }
    }
}