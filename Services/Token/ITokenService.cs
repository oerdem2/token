using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Models.Token;

namespace AuthServer.Services.Token;

public interface ITokenService
{
    public Task<TokenResponse> GenerateToken(TokenRequest tokenRequest);
}
