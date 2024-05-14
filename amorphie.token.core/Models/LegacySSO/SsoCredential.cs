using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.LegacySSO
{
    public class SsoCredential
    {
        public int PropertyId{get;set;}
        public string Name{get;set;}
        public string Value{get;set;}
    }
    
    public class SsoCredentialRoot
    {
        public SsoCredential root{get;set;}
    }
}