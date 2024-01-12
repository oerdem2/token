

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using amorphie.token.core.Enums;
using amorphie.token.core.Models.Token;
using Microsoft.IdentityModel.Tokens;

namespace amorphie.token.core.Helpers;

public class JwtHelper
{
    public static string GenerateJwt(string? issuer = null, string? audience = null, List<Claim>? claims = null, DateTime? notBefore = null, DateTime? expires = null,
    SigningCredentials? signingCredentials = null)
    {
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: expires, signingCredentials: signingCredentials);

        string jwt = handler.WriteToken(token);
        return jwt;
    }

    public static JwtSecurityToken ReadJwt(string token)
    {
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwt = handler.ReadJwtToken(token);

        return jwt;
    }

    public static string? GetClaim(string token, string claimName)
    {
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwt = handler.ReadJwtToken(token);

        var claim = jwt.Claims.FirstOrDefault(c => c.Type == claimName);

        return claim?.Value;
    }

    public static TokenInfo CreateTokenInfo(TokenType tokenType, Guid jti, string clientId, DateTime expiredAt, bool isActive, string reference, List<string> scopes, Guid userId, Guid? relatedTokenId, Guid? consentId)
    {
        var tokenInfo = new TokenInfo
        {
            Scopes = scopes,
            ClientId = clientId,
            Id = jti,
            TokenType = tokenType,
            ExpiredAt = expiredAt,
            IsActive = isActive,
            Reference = reference,
            UserId = userId,
            RelatedTokenId = relatedTokenId,
            ConsentId = consentId
        };

        return tokenInfo;
    }

    public static bool ValidateToken(
    string token,
    string issuer,
    string? audience,
    SecurityKey signingKey,
    out JwtSecurityToken? jwt
    )
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,

        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            jwt = (JwtSecurityToken)validatedToken;

            return true;
        }
        catch (SecurityTokenValidationException ex)
        {
            jwt = null;
            return false;
        }
    }
}
