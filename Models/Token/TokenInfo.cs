using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthServer.Models.Token;

public class TokenInfo
{
    public Guid Id{get;set;}
    public string Jwt{get;set;}
    public bool IsActive { get; set; }
    public List<string> Scopes{ get; set; }
    public string ClientId { get; set; }
    public string Reference { get; set; }
    public DateTime ExpiredAt { get; set; }
}
