using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.InternetBanking
{
    public class IBStatus : IbBaseEntity
    {
        public Guid UserId { get; set; }
        //Active 20, Locked 30
        public int Type { get; set; }
        //Password Block 6
        public int Reason { get; set; }
        public string? ReasonDescription { get; set; }
        //Active 10
        public int? State { get; set; }
    }
}