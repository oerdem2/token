using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.InternetBanking
{
    public class IBRoleDefinition : IbBaseEntity
    {
        public int Key{get;set;}
        public bool IsActive{get;set;}
    }
}