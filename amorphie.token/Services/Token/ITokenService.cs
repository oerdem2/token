
namespace amorphie.token.Services.Token;

public interface ITokenService
{
    public Task<TokenResponse> GenerateToken(TokenRequest tokenRequest);
}
