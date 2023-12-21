

namespace amorphie.token.core.Models.InternetBanking
{
    public class IBPassword : IbBaseEntity
    {
        public Guid UserId { get; set; }
        public string? HashedPassword { get; set; }
        public int? AccessFailedCount { get; set; }
        public bool? MustResetPassword{get;set;}
    }
}