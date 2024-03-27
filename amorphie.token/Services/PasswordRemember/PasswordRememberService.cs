
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Refit;

namespace amorphie.token;

public class PasswordRememberService : ServiceBase, IPasswordRememberService
{
    // private readonly CardValidationOptions options;
    private readonly IPasswordRememberCard _passwordRememberCard;
    public PasswordRememberService(
        ILogger<PasswordRememberService> logger,
        IConfiguration configuration,
        IPasswordRememberCard passwordRememberCard) : base(logger, configuration)
    {
        
        _passwordRememberCard = passwordRememberCard;
    }

    public async Task<ServiceResponse<bool>> HasCardAsync(string citizenshipNo)
    {

        using var httpClient = new HttpClient();
        StringContent request = new(JsonSerializer.Serialize(new CardValidationOptions{
            ClientId = Configuration["CardValidationClientId"],
            ClientSecret = Configuration["CardValidationClientSecret"],
            GrantType = "client_credentials",
            Scopes =new List<string>() { "retail-customer" }
        }), Encoding.UTF8, "application/json");
        var response = new ServiceResponse<bool>();
        var httpResponse = await httpClient.PostAsync(Configuration["CardValidationTokenBaseAddress"], request);
        if (!httpResponse.IsSuccessStatusCode)
        {
            response.StatusCode = 500;
            response.Detail = "Couldn't Get Token For Using Card Service";
            response.Response = false;
            return response;
        }
        var resp = await httpResponse.Content.ReadFromJsonAsync<TokenResponse>();
        try
        {
            var cardResponse = await _passwordRememberCard.GetCards(citizenshipNo, resp.TokenType+" "+resp.AccessToken);
            if (cardResponse is not null && cardResponse.Count > 0)
            {
                response.StatusCode = 200;
                response.Detail = "Customer has cards.";
                response.Response = true;
                return response;
            }
            else
            {
                response.StatusCode = 200;
                response.Detail = "Customer has not cards";
                response.Response = false;
                return response;
            }
        }
        catch (ApiException apiException)
        {
            response.StatusCode = (int)apiException.StatusCode;
            response.Detail = apiException.ToString();
            response.Response = false;
            return response;
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            response.Detail = ex.ToString();
            response.Response = false;
            return response;
        }

    }


    // private Task<string> GetToken()
    // {
    //     return null;
    // }
}
