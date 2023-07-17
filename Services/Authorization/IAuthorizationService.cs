using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Models;
using AuthServer.Models.Account;
using AuthServer.Models.Authorization;
using AuthServer.Models.Token;
using AuthServer.Models.User;
using token.Models;

namespace AuthServer.Services.Authorization;

public interface IAuthorizationService
{
    public Task<ServiceResponse<AuthorizationResponse>> Authorize(AuthorizationRequest request); 
    public Task AssignUserToAuthorizationCode(LoginResponse user,string authorizationCode);
    public Task<ServiceResponse<TokenResponse>> GenerateToken(TokenRequest tokenRequest);
    public Task<ServiceResponse<TokenResponse>> GenerateTokenWithPassword(TokenRequest tokenRequest);
}
