using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.User
{
    public class SecurityQuestionDto
    {
        public Guid Id{get;set;}
        public string? Key { get; set; }
        public string? ValueTypeClr { get; set; }
        public int? Priority { get; set; }
        public bool? IsActive { get; set; }
        public string? DescriptionTr { get; set; }
        public string? DescriptionEn { get; set; }
    }
}