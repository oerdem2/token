using AuthServer.Models.Token;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token
{
    public class DatabaseContext : DbContext
    {
        public DbSet<TokenInfo> Tokens{get;set;}

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {

        }
    }
}