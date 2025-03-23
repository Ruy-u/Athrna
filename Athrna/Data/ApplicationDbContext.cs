using Microsoft.EntityFrameworkCore;
using Athrna.Models;

namespace Athrna.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Client { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<SiteTranslation> SiteTranslations { get; set; }
        public DbSet<CulturalInfoTranslation> CulturalInfoTranslations { get; set; }
        public DbSet<Guide> Guides { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<CulturalInfo> CulturalInfos { get; set; }
        public DbSet<Administrator> Administrators { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<Client>()
                .HasIndex(c => c.Username)
                .IsUnique();

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.Email)
                .IsUnique();
        }
    }
}