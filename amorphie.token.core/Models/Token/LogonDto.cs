using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Token
{
    public class LogonDto
    {
        public DateTime? LastSuccessfullLogonDate{get;set;}
        public DateTime? LastFailedLogonDate{get;set;}
    }
}