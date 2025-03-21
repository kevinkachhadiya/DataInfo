
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace DataInfo.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<UserData> Users { get; set; }

        public DbSet<Country> Countries { get; set; }

        public DbSet<State> states { get; set; }

        public DbSet<City> Cities { get; set; }

        public DbSet<Login> usetlogin { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
}
