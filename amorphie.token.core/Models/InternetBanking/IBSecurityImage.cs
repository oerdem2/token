using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.InternetBanking
{
    public class IBSecurityImage : IbBaseEntity
    {
        public Guid UserId { get; set; }
        public Guid DefinitionId { get; set; }
        public bool? RequireChange { get; set; }
    }
}