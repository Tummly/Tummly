using Microsoft.EntityFrameworkCore;
using TummlyBackend.Models;

namespace TummlyBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options
        ) : base(options)
        {
        }

        /*
         =========================================
         DB TABLES
         =========================================
        */

        public DbSet<User> Users { get; set; }

        public DbSet<Admin> Admins { get; set; }

        public DbSet<TrialRequest> TrialRequests { get; set; }

        public DbSet<PendingTrialRequest> PendingTrialRequests { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<OtpVerification> OtpVerifications { get; set; }

        public DbSet<PasswordReset> PasswordResets { get; set; }

        public DbSet<AccountSetupInvite> AccountSetupInvites { get; set; }

        public DbSet<Restaurant> Restaurants { get; set; }

        public DbSet<RestaurantLocation> RestaurantLocations { get; set; }

        public DbSet<GuestLoopSetup> GuestLoopSetups { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder
        )
        {
            base.OnModelCreating(modelBuilder);

            /*
             =========================================
             USER TABLE
             =========================================
            */

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            /*
             =========================================
             TRIAL REQUEST TABLE
             =========================================
            */

            modelBuilder.Entity<TrialRequest>()
                .HasIndex(t => t.Email)
                .IsUnique(false);

            /*
             =========================================
             PENDING TRIAL REQUEST TABLE
             =========================================
            */

            modelBuilder.Entity<PendingTrialRequest>()
                .HasIndex(t => t.Email)
                .IsUnique(false);

            /*
             =========================================
             OTP TABLE
             =========================================
            */

            modelBuilder.Entity<OtpVerification>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            /*
             =========================================
             REFRESH TOKEN TABLE
             =========================================
            */

            modelBuilder.Entity<RefreshToken>()
                .HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            /*
             =========================================
             PASSWORD RESET TABLE
             =========================================
            */

            modelBuilder.Entity<PasswordReset>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            /*
             =========================================
             RESTAURANT -> LOCATIONS
             =========================================
            */

            modelBuilder.Entity<Restaurant>()
                .HasMany(r => r.Locations)
                .WithOne(l => l.Restaurant)
                .HasForeignKey(l => l.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            /*
             =========================================
             USER -> RESTAURANTS
             =========================================
            */

            modelBuilder.Entity<Restaurant>()
                .HasOne(r => r.OwnerUser)
                .WithMany(u => u.OwnedRestaurants)
                .HasForeignKey(r => r.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);

            /*
             =========================================
             RESTAURANT -> GUEST LOOP
             =========================================
            */

            modelBuilder.Entity<GuestLoopSetup>()
                .HasOne(g => g.Restaurant)
                .WithOne(r => r.GuestLoopSetup)
                .HasForeignKey<GuestLoopSetup>(g => g.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}