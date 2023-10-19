using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.core.Models.Token;

public class RefreshTokenInfo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public List<string> Scopes { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Reference { get; set; }
    public string ReferencePassword { get; set; }
    public DateTime ExpiredAt { get; set; }
}
