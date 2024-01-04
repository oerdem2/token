using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Enums;
using amorphie.token.core.Models.Authorization;
using amorphie.token.core.Models.Consent;
using amorphie.token.core.Models.Profile;
using amorphie.token.core.Models.User;

namespace amorphie.token.core.Models.Transaction
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public ConsentResponse? ConsentData { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionState TransactionState { get; set; }
        public LoginResponse? User { get; set; }
        public UserInfo? UserInfo { get; set; }
        public ProfileResponse? Profile { get; set; }
        public SecondFactorMethod SecondFactorMethod { get; set; }
        public bool Next { get; set; } = false;
        public AuthorizationRequest? AuthorizationReqest { get; set; }
        public TransactionNextEvent TransactionNextEvent { get; set; } = TransactionNextEvent.Waiting;
        public TransactionNextPage TransactionNextPage { get; set; }
        public string? TransactionNextMessage { get; set; }
        public int OtpErrorCount { get; set; } = 0;
        public string? ErrorDetail { get; set; }
    }
}