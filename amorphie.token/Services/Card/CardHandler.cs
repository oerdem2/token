using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Refit;

namespace amorphie.token.Services.Card
{
    public class CardHandler : ServiceBase,ICardHandler
    {
        private ICardService _cardService;
        public CardHandler(ILogger<CardHandler> logger, IConfiguration configuration,ICardService cardService):base(logger,configuration)
        {
            _cardService = cardService;
        }
        public async Task<ServiceResponse> ValidateCard(string reference, string cardNo, string cvv, string pin)
        {
            var response = new ServiceResponse();
            using var httpClient = new HttpClient();
            StringContent request = new(JsonSerializer.Serialize(new TokenRequest
            {
                ClientId = Configuration["SelfClientId"],
                ClientSecret = Configuration["SelfClientSecret"],
                GrantType = "client_credentials",
                Scopes = new List<string>(){"retail-customer"}
            }), Encoding.UTF8, "application/json");

            var httpResponse = await httpClient.PostAsync(Configuration["localAddress"] + "public/Token", request);
            if(!httpResponse.IsSuccessStatusCode)
            {
                response.StatusCode = 500;
                response.Detail = "Couldn't Get Token For Using Card Service";
                return response;
            }
            var resp = await httpResponse.Content.ReadFromJsonAsync<TokenResponse>();
            
            try
            {
                var res = await _cardService.ValidateCard(reference,cardNo,cvv,pin,resp.AccessToken);
                if(res.IsSuccess)
                {
                    response.StatusCode = 200;
                    response.Detail = "Provided Card Info Not Valid";
                    return response;
                }
                else
                {
                    response.StatusCode = 404;
                    response.Detail = "Provided Card Info Not Valid";
                    return response;
                }
            }
            catch (ApiException ex)
            {
                response.StatusCode = (int)ex.StatusCode;
                response.Detail = ex.ToString();
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
                return response;
            }
        }
    }
}