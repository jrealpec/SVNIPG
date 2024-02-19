using Microsoft.EntityFrameworkCore;
using com.tiglobalsas.frontend.Models;

namespace com.tiglobalsas.frontend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext (DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppFile> File { get; set; }
    }
}
