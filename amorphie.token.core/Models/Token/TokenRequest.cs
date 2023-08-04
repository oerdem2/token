using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Token;

public class TokenRequest
{
    public string username{get;set;}
    public string password{get;set;}
    public string client_id { get; set; }
    public string client_secret { get; set; }
    public string code { get; set; }
    public string grant_type { get; set; }
    public string redirect_uri { get; set; }
    public string code_verifier { get; set; }
    public IEnumerable<string> scopes { get; set; }
}
