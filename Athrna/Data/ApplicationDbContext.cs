﻿using Microsoft.EntityFrameworkCore;
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
        public DbSet<Site> Site { get; set; }
        public DbSet<Guide> Guide { get; set; }
        public DbSet<GuideApplication> GuideApplication { get; set; }
        public DbSet<Rating> Rating { get; set; }
        public DbSet<Bookmark> Bookmark { get; set; }
        public DbSet<City> City { get; set; }
        public DbSet<CulturalInfo> CulturalInfo { get; set; }
        public DbSet<Administrator> Administrator { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Booking> Booking { get; set; }
        public DbSet<GuideAvailability> GuideAvailabilities { get; set; }
        public DbSet<Translation> Translation { get; set; }
        public DbSet<Service> Service { get; set; }
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

            // Configure the relationship between Guide and City
            modelBuilder.Entity<Guide>()
                .HasOne(g => g.City)
                .WithMany(c => c.Guides)
                .HasForeignKey(g => g.CityId);

            // Configure the relationship between GuideApplication and City
            modelBuilder.Entity<GuideApplication>()
                .HasOne(ga => ga.City)
                .WithMany()
                .HasForeignKey(ga => ga.CityId);

            // Ensure Site no longer has a relationship with Guide
            modelBuilder.Entity<Site>()
                .HasOne(s => s.City)
                .WithMany(c => c.Sites)
                .HasForeignKey(s => s.CityId);

            // Map entity names to singular table names
            modelBuilder.Entity<City>().ToTable("City");
            modelBuilder.Entity<Site>().ToTable("Site");
            modelBuilder.Entity<Guide>().ToTable("Guide");
            modelBuilder.Entity<GuideApplication>().ToTable("GuideApplication");
            modelBuilder.Entity<Rating>().ToTable("Rating");
            modelBuilder.Entity<Bookmark>().ToTable("Bookmark");
            modelBuilder.Entity<CulturalInfo>().ToTable("CulturalInfo");
            modelBuilder.Entity<Administrator>().ToTable("Administrator");
            modelBuilder.Entity<Message>().ToTable("Message");
            modelBuilder.Entity<Booking>().ToTable("Booking");
            modelBuilder.Entity<GuideAvailability>().ToTable("GuideAvailability");
            modelBuilder.Entity<Service>().ToTable("Service");

            modelBuilder.Entity<Translation>()
                .HasIndex(t => new { t.SourceLanguage, t.TargetLanguage, t.TextHash })
                .IsUnique();

            // Set up the relationship between Message and Client
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Client)
                .WithMany()
                .HasForeignKey("ClientId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Set up the relationship between Message and Guide
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Guide)
                .WithMany()
                .HasForeignKey("GuideId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<Service>()
                .HasOne(s => s.Site)
                .WithMany(s => s.Services)
                .HasForeignKey(s => s.SiteId);
        }
    }
}