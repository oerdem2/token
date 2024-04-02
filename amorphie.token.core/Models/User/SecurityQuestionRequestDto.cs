using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.User
{
    public class SecurityQuestionRequestDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid CreatedByBehalfOf { get; set; }
        public DateTime ModifiedAt { get; set; }
        public Guid ModifiedBy { get; set; }
        public Guid ModifiedByBehalfOf { get; set; }
        public bool IsActive { get; set; }
        public string Key { get; set; }
        public int Priority { get; set; }
        public string ValueTypeClr { get; set; }
        public string DescriptionTr { get; set; }
        public string DescriptionEn { get; set; }
    }
}