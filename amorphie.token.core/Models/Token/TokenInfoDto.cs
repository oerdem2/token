using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Token;

public class TokenInfoDto
{
    
    public DateTime IssuedAt{get;set;}
    public Guid? UserId{get;set;}
    public string? Reference{get;set;}
    public bool IsActive {get;set;}
    public ICollection<string>? Scopes{get;set;}
    public string? ClientId{get;set;}
    public DateTime ExpiredAt{get;set;}
}
