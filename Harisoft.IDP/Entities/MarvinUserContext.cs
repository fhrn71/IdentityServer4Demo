using Microsoft.EntityFrameworkCore;

namespace Harisoft.IDP.Entities
{
    public class MarvinUserContext : DbContext
    {
        public MarvinUserContext(DbContextOptions<MarvinUserContext> options)
           : base(options)
        {
           
        }

        public DbSet<User> Users { get; set; }
    }
}
