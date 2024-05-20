using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.InternetSecurityBanking
{
    public class IbSecurityUser
    {
        public string UserName{get;set;}
        public string PasswordHash{get;set;}
        public string IntegrationUserName{get;set;}
        public string Email{get;set;}
    }
}