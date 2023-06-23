using AuthServer.Enums;
using AuthServer.Exceptions;
using AuthServer.Models.Client;
using AuthServer.Models.User;
using Dapr.Client;

namespace AuthServer.Services.Client;

public class ClientService : ServiceBase,IClientService
{
    private readonly DaprClient _daprClient;
    public ClientService(DaprClient daprClient,IConfiguration configuration,ILogger<ClientService> logger):base(logger,configuration)
    {
        _daprClient = daprClient;
    }

    public async Task<ClientResponse> CheckClient(string clientId)
    {

        try
        {
            var client = await _daprClient.InvokeMethodAsync<ClientResponse>(HttpMethod.Get,Configuration["ClientServiceAppName"],"client/"+clientId);
            if(client == null)
            {
                throw new ServiceException((int)Errors.InvalidClient,"Client not found with provided ClientId");
            }         
            return client;   
        }
        catch(InvocationException ex)
        {
            Logger.LogError("Dapr Service Invocation Failed | Detail:"+ex.ToString());
        }
        catch (System.Exception ex)
        {
            Logger.LogError("An Error Occured At Client Invocation | Detail:"+ex.ToString());
        }
       
        

        return null;
    }

    public async Task<ClientResponse> ValidateClient(string clientId, string clientSecret)
    {
        try
        {
            var client = await _daprClient.InvokeMethodAsync<ValidateClientRequest,ClientResponse>(Configuration["ClientServiceAppName"],"/client/validate",
            new()
            {
                ClientId = clientId,
                Secret = clientSecret
            });

            if(client == null)
            {
                throw new ServiceException((int)Errors.InvalidClient,"Client not validated with provided ClientId and ClientSecret");
            }         
            return client;   
        }
        catch(InvocationException ex)
        {
            Logger.LogError("Dapr Service Invocation Failed | Detail:"+ex.Message);
        }
        catch (System.Exception ex)
        {
            Logger.LogError("An Error Occured At Client Invocation | Detail:"+ex.Message);
        }
       
        

        return null;
    }
}
