using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Models.Account;
using AuthServer.Models.User;

namespace AuthServer.Models.Authorization;

public class AuthorizationCode
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RedirectUri { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public bool IsOpenId { get; set; }
    public IList<string> RequestedScopes { get; set; }

    public LoginResponse Subject { get; set; }
    public string CodeChallenge { get; set; }
    public string CodeChallengeMethod { get; set; }
    public string Nonce{get;set;}
}
