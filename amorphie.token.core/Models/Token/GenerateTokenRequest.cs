using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Token;

public class GenerateTokenRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Code { get; set; }
    public string? RefreshToken { get; set; }
    public string? GrantType { get; set; }
    public string? RedirectUri { get; set; }
    public string? CodeVerifier { get; set; }
    public string? RecordId { get; set; }
    public IEnumerable<string>? Scopes { get; set; }
    public Guid? ConsentId { get; set; }

}
