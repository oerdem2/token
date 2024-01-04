using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.MessagingGateway
{
    public class PhoneString
    {
        public string CountryCode { get; set; }
        public string Prefix { get; set; }
        public string Number { get; set; }
    }
}