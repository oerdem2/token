using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthServer.Models.Token;

public class TokenInfo
{
    public bool IsActive { get; set; }
    public List<string> Scopes{ get; set; }
    public Guid ClientId { get; set; }
    public string Reference { get; set; }
    public long ExpiredAt { get; set; }
}
