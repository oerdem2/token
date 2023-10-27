using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.User
{
    public class UserInfo
    {
        public string firstName{get;set;}
        public string lastName{get;set;}
        public string reference{get;set;}
        public string password{get;set;}
        public string eMail{get;set;}
        public UserPhone phone{get;set;}
        public string state{get;set;}
        public string salt{get;set;}
        public bool isArgonHash{get;set;}
        public List<string> tags{get;set;}
        public string reason{get;set;}
        public string explanation{get;set;}
    }

    public class UserPhone
    {
        public string countryCode{get;set;}
        public string prefix{get;set;}
        public string number{get;set;}
    }
}