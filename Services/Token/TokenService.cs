using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Enums;
using AuthServer.Exceptions;
using AuthServer.Models.MockData;
using AuthServer.Models.Token;

namespace AuthServer.Services.Token;

public class TokenService : ITokenService
{
    public Task<TokenResponse> GenerateToken(TokenRequest tokenRequest)
    {
        return null;

        
    }
}
