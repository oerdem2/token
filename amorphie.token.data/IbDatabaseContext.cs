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
        public IbDatabaseContext(DbContextOptions<IbDatabaseContext> options) : base(options)
        {

        }
    }


}