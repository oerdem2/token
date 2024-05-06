using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.InternetBanking
{
    public class IBRole : IbBaseEntity
    {
        public Guid UserId{get;set;}
        public int Channel{get;set;}
        public int Status{get;set;}
        public Guid DefinitionId{get;set;}
    }
}