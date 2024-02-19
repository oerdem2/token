using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Token
{
    public class LogonDetailDto
    {
        public DateTime LogonDate{get;set;}
        public string Channel{get;set;}
        public int Status{get;set;}
    }
}