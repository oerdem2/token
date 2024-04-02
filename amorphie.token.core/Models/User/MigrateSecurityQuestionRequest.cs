using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.User
{
    public class MigrateSecurityQuestionRequest
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SecurityQuestionId { get; set; }
        public string Answer { get; set; }
        public int QuestionStatusType { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? CreatedByBehalfOf { get; set; }
        public DateTime ModifiedAt { get; set; }
        public Guid ModifiedBy { get; set; }
        public Guid? ModifiedByBehalfOf { get; set; }

    }
}