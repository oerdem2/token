using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Authorization
{
    public class CreatePreLoginRequest
    {
        public string clientCode{get;set;}
        public string scopeUser{get;set;}
        public string CodeChallange{get;set;}
        public string? Nonce{get;set;}
        public string State{get;set;}
    }
}