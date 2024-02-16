using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Token
{
    public class FailedLogonDto
    {
        public DateTime LastFailedLogonDate{get;set;}
        public string Channel{get;set;}
    }
}