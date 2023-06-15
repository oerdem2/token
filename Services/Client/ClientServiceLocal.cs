using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Enums;
using AuthServer.Exceptions;
using AuthServer.Models.Client;
using AuthServer.Models.MockData;

namespace AuthServer.Services.Client;

public class ClientServiceLocal : IClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    public ClientServiceLocal(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ClientResponse> CheckClient(string clientId)
    {

        var httpClient = _httpClientFactory.CreateClient("Client");
        var httpResponseMessage = await httpClient.GetAsync(
            "client/"+clientId);
        if(httpResponseMessage.IsSuccessStatusCode)
        {
            var client = await httpResponseMessage.Content.ReadFromJsonAsync<ClientResponse>();
            if(client == null)
            {
                throw new ServiceException((int)Errors.InvalidClient,"Client not found with provided ClientId");
            }         
            return client;   
        }
        else
        {
            throw new ServiceException((int)Errors.InvalidClient,"Client Endpoint Did Not Response Successfully");
        }

        

        return null;
    }

    public async Task<ClientResponse> ValidateClient(string clientId, string clientSecret)
    {
        await Task.CompletedTask;
        return new ClientResponse();
    }
}
