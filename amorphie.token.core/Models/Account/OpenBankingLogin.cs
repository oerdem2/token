using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Account;

public class OpenBankingLogin
{
    public Guid consentId { get; set; }
    public string username { get; set; }
    public string password { get; set; }
}
