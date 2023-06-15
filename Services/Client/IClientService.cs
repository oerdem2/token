
using AuthServer.Models.Client;

namespace AuthServer.Services.Client;

public interface IClientService
{
    public Task<ClientResponse> CheckClient(string clientId);
    public Task<ClientResponse> ValidateClient(string clientId,string clientSecret);
}
