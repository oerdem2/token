using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.token.core.Enums;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.core.Models.Token;

[Index(nameof(UserId))]
public class TokenInfo
{
    public Guid Id{get;set;}
    public TokenType TokenType{get;set;}
    public Guid UserId{get;set;}
    public DateTime IssuedAt{get;set;} = DateTime.UtcNow;
    public string Jwt{get;set;}
    public bool IsActive { get; set; }
    public List<string> Scopes{ get; set; }
    public string ClientId { get; set; }
    public string Reference { get; set; }
    public DateTime ExpiredAt { get; set; }
    public Guid? RelatedTokenId{get;set;}
}
