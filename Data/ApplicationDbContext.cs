using BloodDonationSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet properties for each entity
        public DbSet<User> Users { get; set; }
        public DbSet<BloodType> BloodTypes { get; set; }
        public DbSet<Donor> Donors { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<BloodRequest> BloodRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Donor)
                .WithOne(d => d.User)
                .HasForeignKey<Donor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure BloodType entity
            modelBuilder.Entity<BloodType>()
                .HasIndex(bt => bt.TypeName)
                .IsUnique();

            // Configure Donor entity
            modelBuilder.Entity<Donor>()
                .HasOne(d => d.BloodType)
                .WithMany(bt => bt.Donors)
                .HasForeignKey(d => d.BloodTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Donation entity
            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Donor)
                .WithMany(don => don.Donations)
                .HasForeignKey(d => d.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure BloodRequest entity
            modelBuilder.Entity<BloodRequest>()
                .HasOne(br => br.BloodType)
                .WithMany(bt => bt.BloodRequests)
                .HasForeignKey(br => br.BloodTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Blood Types
            modelBuilder.Entity<BloodType>().HasData(
                new BloodType { Id = 1, TypeName = "A+", Description = "A Positive" },
                new BloodType { Id = 2, TypeName = "A-", Description = "A Negative" },
                new BloodType { Id = 3, TypeName = "B+", Description = "B Positive" },
                new BloodType { Id = 4, TypeName = "B-", Description = "B Negative" },
                new BloodType { Id = 5, TypeName = "AB+", Description = "AB Positive (Universal Receiver)" },
                new BloodType { Id = 6, TypeName = "AB-", Description = "AB Negative" },
                new BloodType { Id = 7, TypeName = "O+", Description = "O Positive" },
                new BloodType { Id = 8, TypeName = "O-", Description = "O Negative (Universal Donor)" }
            );

            // Seed Admin User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "System Administrator",
                    Email = "admin@bloodbank.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = "Admin",
                    Phone = "1234567890",
                    CreatedAt = DateTime.Now,
                    IsActive = true
                }
            );
        }
    }
}
