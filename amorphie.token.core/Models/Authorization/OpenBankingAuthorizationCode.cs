using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.Account;
using amorphie.token.core.Models.User;

namespace amorphie.token.core.Models.Authorization;

public class OpenBankingAuthorizationCode
{
    public string? ConsentId {get;set;}
    public string? Reference{get;set;}
}
