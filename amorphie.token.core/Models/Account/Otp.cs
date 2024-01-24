using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Models.Account
{
    public class Otp
    {
        public string OtpValue { get; set; }
        public string Phone { get; set; }
        public Guid transactionId { get; set; }
        public Guid consentId { get; set; }
        public string consentType { get; set; }
        public bool HasError { get; set; } = false;
        public string ErrorMessage { get; set; } = String.Empty;
    }
}