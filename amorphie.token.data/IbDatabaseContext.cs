using amorphie.token.core.Models.InternetBanking;
using amorphie.token.core.Models.Token;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace amorphie.token.data
{

    public class IbDatabaseContext : DbContext
    {
        public DbSet<IBUser> User { get; set; }
        public DbSet<IBPassword> Password { get; set; }
        public DbSet<IBQuestionDefinition> QuestionDefinition { get; set; }
        public DbSet<IBQuestion> Question { get; set; }
        public DbSet<IBSecurityImageDefinition> SecurityImageDefinition { get; set; }
        public DbSet<IBSecurityImage> SecurityImage { get; set; }
        public DbSet<IBStatus> Status { get; set; }
        public DbSet<IBUserDevice> UserDevice { get; set; }
        public DbSet<IBRole> Role { get; set; }
        public DbSet<IBRoleDefinition> RoleDefinition { get; set; }
        public IbDatabaseContext(DbContextOptions<IbDatabaseContext> options) : base(options)
        {

        }
    }


}