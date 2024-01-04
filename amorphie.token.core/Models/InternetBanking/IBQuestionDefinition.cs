
namespace amorphie.token.core.Models.InternetBanking
{
    public class IBQuestionDefinition : IbBaseEntity
    {
        public Guid Id { get; set; }
        public string? Key { get; set; }
        public string? DescriptionTr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? ValueTypeClr { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public int Type { get; set; }
    }
}