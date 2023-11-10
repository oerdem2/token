using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Enums;
using amorphie.token.core.Models.Profile;
using amorphie.token.core.Models.User;

namespace amorphie.token.core.Models.Transaction
{
    public class Transaction
    {
        public Guid Id{get;set;}
        public Guid ConsentId{get;set;}
        public TransactionType TransactionType{get;set;}
        public TransactionState TransactionState{get;set;}
        public LoginResponse User{get;set;}
        public UserInfo UserInfo{get;set;}
        public ProfileResponse Profile{get;set;}
        public string Request{get;set;}
        public string Response{get;set;}
    }
}