using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthServer.Models.Account;

public class Login
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string RedirectUri { get; set; }
    public string Code { get; set; }
    public IList<string> RequestedScopes { get; set; }
}
