

using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace amorphie.token.Services.Client;

public class ClientService : ServiceBase, IClientService
{
    private readonly DaprClient _daprClient;
    public ClientService(DaprClient daprClient, IConfiguration configuration, ILogger<ClientService> logger) : base(logger, configuration)
    {
        _daprClient = daprClient;
    }

    public async Task<ServiceResponse<ClientResponse>> CheckClient(string clientId)
    {
        try
        {
            var client = await _daprClient.InvokeMethodAsync<ClientResponse>(HttpMethod.Get, Configuration["ClientServiceAppName"], "client/" + clientId);
            if (client == null)
            {
                return new ServiceResponse<ClientResponse>()
                {
                    StatusCode = 460,
                    Detail = "Client Not Found"
                };
            }
            return new ServiceResponse<ClientResponse>()
            {
                StatusCode = 200,
                Response = client
            };
        }
        catch (InvocationException ex)
        {
            Logger.LogError("Dapr Service Invocation Failed | Detail:" + ex.ToString());
            if ((int)ex.Response.StatusCode >= 400 && (int)ex.Response.StatusCode < 500)
            {
                if ((int)ex.Response.StatusCode == 460)
                {
                    return new ServiceResponse<ClientResponse>()
                    {
                        StatusCode = 460,
                        Detail = "Client Not Found"
                    };
                }

            }
            else
            {
                return new ServiceResponse<ClientResponse>()
                {
                    StatusCode = 500,
                    Detail = "Server Error"
                };
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError("An Error Occured At Client Invocation | Detail:" + ex.ToString());
            return new ServiceResponse<ClientResponse>()
            {
                StatusCode = 500,
                Detail = "Server Error"
            };
        }
        return new ServiceResponse<ClientResponse>()
        {
            StatusCode = 500,
            Detail = "Server Error"
        };

    }

    public async Task<ServiceResponse<ClientResponse>> ValidateClient(string clientId, string clientSecret)
    {
        try
        {
            var client = await _daprClient.InvokeMethodAsync<ValidateClientRequest, ClientResponse>(Configuration["ClientServiceAppName"], "/client/validate",
            new()
            {
                ClientId = clientId,
                Secret = clientSecret
            });

            if (client == null)
            {
                return new ServiceResponse<ClientResponse>()
                {
                    StatusCode = 460,
                    Detail = "Client Not Found"
                };
            }
            return new ServiceResponse<ClientResponse>()
            {
                StatusCode = 200,
                Response = client
            };
        }
        catch (InvocationException ex)
        {
            Logger.LogError("Dapr Service Invocation Failed | Detail:" + ex.ToString());
            if ((int)ex.Response.StatusCode >= 400 && (int)ex.Response.StatusCode < 500)
            {
                if ((int)ex.Response.StatusCode == 460)
                {
                    return new ServiceResponse<ClientResponse>()
                    {
                        StatusCode = 460,
                        Detail = "Client Not Found"
                    };
                }

            }
            else
            {
                return new ServiceResponse<ClientResponse>()
                {
                    StatusCode = 500,
                    Detail = "Server Error"
                };
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError("An Error Occured At Client Invocation | Detail:" + ex.Message);
            return new ServiceResponse<ClientResponse>()
            {
                StatusCode = 500,
                Detail = "Server Error"
            };
        }

        return new ServiceResponse<ClientResponse>()
        {
            StatusCode = 500,
            Detail = "Server Error"
        };
    }
}
