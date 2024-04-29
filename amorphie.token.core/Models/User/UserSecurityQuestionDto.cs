using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.User
{
    public class UserSecurityQuestionDto
    {
        public Guid Id{get;set;}
        public QuestionStatusType? Status { get; set; }
    }

    public enum QuestionStatusType
    {

        Active = 10,
        Deactive = 20,
        Blocked = 30,
        WaitingApproval = 40,
        NotApproved = 50
    }
}