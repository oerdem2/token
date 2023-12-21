

namespace amorphie.token.core.Models.InternetBanking
{
    public class IBQuestion : IbBaseEntity
    {
        public Guid UserId{get;set;}
        public Guid DefinitionId { get; set; }
        public string? EncryptedAnswer { get; set; }
        public int Status { get; set; }
        public string? ChangeReason { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime? ChangeDate { get; set; }
        public string? ApproveReason { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApproveDate { get; set; }
        public int? AccessFailedCount { get; set; }
        public DateTime? LastAccessDate { get; set; }
        public DateTime? LastVerificationDate { get; set; }
    }
}