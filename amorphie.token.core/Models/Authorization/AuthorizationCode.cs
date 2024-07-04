using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Account;
using amorphie.token.core.Models.Profile;
using amorphie.token.core.Models.User;

namespace amorphie.token.core.Models.Authorization;

public class AuthorizationCode
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUri { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public bool IsOpenId { get; set; }
    public IList<string>? RequestedScopes { get; set; }

    public LoginResponse? Subject { get; set; }
    public SimpleProfileResponse? Profile{get;set;}
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? Nonce { get; set; }
    public string? ConsentId { get; set; }
    public string? State{get;set;}
    public core.Models.Collection.User? CollectionUser{get;set;}
}
