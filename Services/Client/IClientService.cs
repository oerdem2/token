
using AuthServer.Models.Client;
using token.Models;

namespace AuthServer.Services.Client;

public interface IClientService
{
    public Task<ServiceResponse<ClientResponse>> CheckClient(string clientId);
    public Task<ServiceResponse<ClientResponse>> ValidateClient(string clientId,string clientSecret);
}
