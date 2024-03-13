using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.core.Models.InternetBanking
{
    public class IBUser : IbBaseEntity
    {
        public string UserName { get; set; }
    }

}