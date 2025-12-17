using Microsoft.EntityFrameworkCore;
using NFSE_ABRASF.Models;

namespace NFSE_ABRASF.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<Empresas>? Empresas { get; set; }
    }
}
