

using System.Collections.Generic;

namespace amorphie.token.core.Models.Client;

public class Token
{
    public int type { get; set; }
    public string? duration { get; set; }
    public List<string>? claims { get; set; }
    public List<string>? privateClaims { get; set; }
}
