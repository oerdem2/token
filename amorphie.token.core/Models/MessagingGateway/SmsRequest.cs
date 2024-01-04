using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Enums.MessagingGateway;

namespace amorphie.token.core.Models.MessagingGateway
{
    public class SmsRequest
    {

        public SenderType Sender { get; set; }
        public SmsTypes SmsType { get; set; }
        public PhoneString Phone { get; set; }
        public string Content { get; set; }
        public long? CustomerNo { get; set; }
        public string? CitizenshipNo { get; set; }
        public Process Process { get; set; }
    }
}