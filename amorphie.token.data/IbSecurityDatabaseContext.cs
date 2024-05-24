using amorphie.token.core.Models.InternetBanking;
using amorphie.token.core.Models.InternetSecurityBanking;
using amorphie.token.core.Models.Token;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace amorphie.token.data
{

    public class IbSecurityDatabaseContext : DbContext
    {
        public DbSet<IbSecurityUser> AspNetUsers { get; set; }
        public IbSecurityDatabaseContext(DbContextOptions<IbSecurityDatabaseContext> options) : base(options)
        {

        }
    }


}