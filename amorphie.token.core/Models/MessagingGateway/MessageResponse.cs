using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.MessagingGateway
{
    public class MessageResponse
    {
        public Guid TxnId { get; set; }
        public string Status { get; set; }
    }
}