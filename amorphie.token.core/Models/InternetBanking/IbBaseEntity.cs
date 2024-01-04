
namespace amorphie.token.core.Models.InternetBanking
{
    public abstract class IbBaseEntity : IEntity
    {
        public Guid? CreatedByInstanceId
        {
            get;
            set;
        }

        public string? CreatedByInstanceState
        {
            get;
            set;
        }

        public virtual string? CreatedByUserName
        {
            get;
            set;
        }

        public virtual string? CreatedByIP
        {
            get;
            set;
        }

        public virtual string? CreatedBehalfOf
        {
            get;
            set;
        }

        protected IbBaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
        }
    }
}