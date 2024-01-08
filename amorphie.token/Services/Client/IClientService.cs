

namespace amorphie.token.Services.Client;

public interface IClientService
{
    public Task<ServiceResponse<ClientResponse>> CheckClient(string clientId);
    public Task<ServiceResponse<ClientResponse>> ValidateClient(string clientId,string clientSecret);
    public Task<ServiceResponse<ClientResponse>> CheckClientByCode(string clientCode);
    public Task<ServiceResponse<ClientResponse>> ValidateClientByCode(string clientCode,string clientSecret);
}
