

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using amorphie.token.core.Models.Token;
using Microsoft.IdentityModel.Tokens;

namespace amorphie.token.core.Helpers;

public class JwtHelper
{
    public static string GenerateJwt(string? issuer = null, string? audience = null, List<Claim>? claims = null, DateTime? notBefore = null, DateTime? expires = null,
    SigningCredentials signingCredentials = null)
    {
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: expires, signingCredentials: signingCredentials);


        string jwt = handler.WriteToken(token);
        return jwt;
    }

    public static TokenInfo CreateTokenInfo(Guid jti, string clientId, DateTime expiredAt, bool isActive, string jwt, string reference, List<string> scopes, Guid userId)
    {
        var tokenInfo = new TokenInfo();
        tokenInfo.Id = jti;
        tokenInfo.ClientId = clientId;
        tokenInfo.ExpiredAt = expiredAt;
        tokenInfo.IsActive = isActive;
        tokenInfo.Jwt = jwt;
        tokenInfo.Reference = reference;
        tokenInfo.Scopes = scopes;
        tokenInfo.UserId = userId;

        return tokenInfo;
    }
}
