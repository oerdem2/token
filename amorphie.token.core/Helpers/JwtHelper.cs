

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace amorphie.token.core.Helpers;

public class JwtHelper
{
    public static string GenerateJwt(string? issuer = null,string? audience = null,List<Claim>? claims = null,DateTime? notBefore = null,DateTime? expires = null,
    SigningCredentials signingCredentials = null)
    {
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: expires, signingCredentials:signingCredentials);
        
        
        string jwt = handler.WriteToken(token);
        return jwt;
    }
}
