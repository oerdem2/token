using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Token;

public class TokenResponse
{
    public string access_token { get; set; }
    public string id_token { get; set; }
    public string token_type { get; set; }
    public int expires{get;set; }
}
