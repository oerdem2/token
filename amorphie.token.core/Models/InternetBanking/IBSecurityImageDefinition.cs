

namespace amorphie.token.core.Models.InternetBanking
{
    public class IBSecurityImageDefinition : IbBaseEntity
    {
        public string? ImagePath { get; set; }
        public string? TitleTr { get; set; }
        public string? TitleEn { get; set; }
        public bool IsActive { get; set; }
    }
}