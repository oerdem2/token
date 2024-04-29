using Refit;

namespace amorphie.token;

public interface IPasswordRememberCard
{
    [Get("/{citizenShipNo}")]
    Task<List<object>> GetCards(string citizenShipNo, [Header("Authorization")] string token);
}
