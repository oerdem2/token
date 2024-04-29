using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.User
{
    public class SecurityImageDto
    {
        public Guid Id{get;set;}
        public string Image { get; set; } 
        public string EnTitle { get; set; } 
        public string TrTitle { get; set; } 
        public bool IsSelected{get;set;}
    }
}