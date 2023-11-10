using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Account
{
    public class Otp
    {
        public string OtpValue{get;set;}
        public string Phone{get;set;}
        public string transactionId{get;set;}
    }
}