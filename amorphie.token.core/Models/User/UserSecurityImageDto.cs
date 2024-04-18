using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.User
{
    public class UserSecurityImageDto
    {
        public Guid Id{get;set;}
        public bool? RequireChange { get; set; }
    }
}