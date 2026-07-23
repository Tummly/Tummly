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

        public DbSet<TrustedDevice> TrustedDevices { get; set; }

        public DbSet<Feedback> Feedbacks { get; set; }

        public DbSet<MasterGuest> MasterGuests { get; set; }

        public DbSet<LocationGuest> LocationGuests { get; set; }

        public DbSet<GuestTag> GuestTags { get; set; }

        public DbSet<LocationGuestTag> LocationGuestTags { get; set; }

        public DbSet<LocationGuestActivityEvent> LocationGuestActivityEvents
        {
            get;
            set;
        }

        public DbSet<LocationGuestNote> LocationGuestNotes { get; set; }

        public DbSet<DataMigrationMarker> DataMigrationMarkers { get; set; }

        public DbSet<HelpCentreQuery> HelpCentreQueries { get; set; }

        public DbSet<HelpCentreQueryMessage> HelpCentreQueryMessages { get; set; }

        public DbSet<HelpCentreQueryAttachment> HelpCentreQueryAttachments { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<NotificationPreference> NotificationPreferences { get; set; }

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

            modelBuilder.Entity<TrialRequest>()
                .Property(t => t.Status)
                .HasConversion(
                    v => v.ToWireString(),
                    v => TrialRequestStatusExtensions.FromWireString(v)
                );

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
             RESTAURANT LOCATION -> LINK TOKEN (unique)
             =========================================
             */

            modelBuilder.Entity<RestaurantLocation>()
                .HasIndex(l => l.LinkToken)
                .IsUnique();

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

            /*
             =========================================
             USER -> TRUSTED DEVICES
             =========================================
             */

            modelBuilder.Entity<TrustedDevice>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrustedDevice>()
                .HasIndex(t => new { t.UserId, t.TokenHash })
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.SelectedLocation)
                .WithMany()
                .HasForeignKey(u => u.SelectedLocationId)
                .OnDelete(DeleteBehavior.NoAction);

            /*
             =========================================
             RESTAURANT LOCATION -> FEEDBACK
             =========================================
             */

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.RestaurantLocation)
                .WithMany()
                .HasForeignKey(f => f.RestaurantLocationId)
                .OnDelete(DeleteBehavior.Cascade);

            /*
             =========================================
             MASTER GUEST / LOCATION GUEST
             =========================================
            */

            modelBuilder.Entity<MasterGuest>()
                .HasOne(g => g.Restaurant)
                .WithMany()
                .HasForeignKey(g => g.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MasterGuest>()
                .HasIndex(g => new { g.RestaurantId, g.NormalizedEmail })
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL");

            modelBuilder.Entity<MasterGuest>()
                .HasIndex(g => new { g.RestaurantId, g.NormalizedPhone })
                .IsUnique()
                .HasFilter("[NormalizedPhone] IS NOT NULL");

            modelBuilder.Entity<LocationGuest>()
                .HasOne(lg => lg.MasterGuest)
                .WithMany(g => g.LocationGuests)
                .HasForeignKey(lg => lg.MasterGuestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LocationGuest>()
                .HasOne(lg => lg.RestaurantLocation)
                .WithMany()
                .HasForeignKey(lg => lg.RestaurantLocationId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LocationGuest>()
                .HasIndex(lg => new { lg.MasterGuestId, lg.RestaurantLocationId })
                .IsUnique();

            modelBuilder.Entity<LocationGuest>()
                .HasIndex(lg => new { lg.RestaurantLocationId, lg.CreatedAt });

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.LocationGuest)
                .WithMany(lg => lg.Feedbacks)
                .HasForeignKey(f => f.LocationGuestId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            /*
             =========================================
             GUEST TAG CATALOG / MEMBERSHIP
             =========================================
            */

            modelBuilder.Entity<GuestTag>()
                .HasOne(t => t.Restaurant)
                .WithMany()
                .HasForeignKey(t => t.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GuestTag>()
                .HasIndex(t => new { t.RestaurantId, t.NormalizedName })
                .IsUnique();

            modelBuilder.Entity<GuestTag>()
                .HasIndex(t => new { t.RestaurantId, t.DetectedTagKey })
                .IsUnique()
                .HasFilter("[DetectedTagKey] IS NOT NULL");

            modelBuilder.Entity<LocationGuestTag>()
                .HasKey(m => new { m.LocationGuestId, m.GuestTagId });

            // LocationGuest → membership is NoAction: SQL Server rejects dual
            // CASCADE paths (Restaurant→GuestTags→memberships and
            // Restaurant→MasterGuest→LocationGuest→memberships). Guest delete
            // removes memberships in LocationGuestDeleteService; GuestTag
            // delete still cascades memberships.
            modelBuilder.Entity<LocationGuestTag>()
                .HasOne(m => m.LocationGuest)
                .WithMany(lg => lg.GuestTags)
                .HasForeignKey(m => m.LocationGuestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LocationGuestTag>()
                .HasOne(m => m.GuestTag)
                .WithMany(t => t.Memberships)
                .HasForeignKey(m => m.GuestTagId)
                .OnDelete(DeleteBehavior.Cascade);

            /*
             =========================================
             LOCATION GUEST ACTIVITY EVENTS
             =========================================
            */

            modelBuilder.Entity<LocationGuestActivityEvent>()
                .HasOne(e => e.LocationGuest)
                .WithMany()
                .HasForeignKey(e => e.LocationGuestId)
                // NoAction: SQL Server rejects dual SET NULL paths
                // (Restaurant→MasterGuest→LG and Restaurant→Locations→Feedback).
                // Guest delete removes guest-scoped rows in LocationGuestDeleteService.
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<LocationGuestActivityEvent>()
                .HasOne(e => e.Feedback)
                .WithMany()
                .HasForeignKey(e => e.FeedbackId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            modelBuilder.Entity<LocationGuestActivityEvent>()
                .HasIndex(e => new { e.LocationGuestId, e.OccurredAt });

            modelBuilder.Entity<LocationGuestActivityEvent>()
                .HasIndex(e => new { e.FeedbackId, e.OccurredAt });

            modelBuilder.Entity<LocationGuestActivityEvent>()
                .HasIndex(e => e.Kind);

            /*
             =========================================
             LOCATION GUEST NOTES
             =========================================
            */

            modelBuilder.Entity<LocationGuestNote>()
                .HasOne(n => n.LocationGuest)
                .WithMany(lg => lg.Notes)
                .HasForeignKey(n => n.LocationGuestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LocationGuestNote>()
                .HasOne(n => n.AuthorUser)
                .WithMany()
                .HasForeignKey(n => n.AuthorUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            modelBuilder.Entity<LocationGuestNote>()
                .HasIndex(n => new { n.LocationGuestId, n.CreatedAt });

            /*
             =========================================
             DATA MIGRATION MARKERS (startup backfill watermarks)
             =========================================
            */

            modelBuilder.Entity<DataMigrationMarker>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<DataMigrationMarker>()
                .Property(m => m.Id)
                .HasMaxLength(64);

            /*
             =========================================
             HELP CENTRE QUERY
             =========================================
            */

            modelBuilder.Entity<HelpCentreQuery>()
                .Property(q => q.Topic)
                .HasConversion(
                    v => v.ToSlug(),
                    v => HelpCentreQueryTopicExtensions.FromSlug(v)
                );

            modelBuilder.Entity<HelpCentreQuery>()
                .Property(q => q.Status)
                .HasConversion(
                    v => v.ToWireString(),
                    v => HelpCentreQueryStatusExtensions.FromWireString(v)
                );

            modelBuilder.Entity<HelpCentreQuery>()
                .HasOne(q => q.User)
                .WithMany()
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HelpCentreQuery>()
                .HasOne(q => q.RestaurantLocation)
                .WithMany()
                .HasForeignKey(q => q.RestaurantLocationId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HelpCentreQuery>()
                .HasIndex(q => q.Status);

            modelBuilder.Entity<HelpCentreQuery>()
                .HasIndex(q => q.UpdatedAt);

            modelBuilder.Entity<HelpCentreQueryMessage>()
                .Property(m => m.AuthorKind)
                .HasConversion(
                    v => v.ToWireString(),
                    v => HelpCentreQueryAuthorKindExtensions.FromWireString(v)
                );

            modelBuilder.Entity<HelpCentreQueryMessage>()
                .HasOne(m => m.Query)
                .WithMany(q => q.Messages)
                .HasForeignKey(m => m.QueryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HelpCentreQueryAttachment>()
                .HasOne(a => a.Query)
                .WithMany(q => q.Attachments)
                .HasForeignKey(a => a.QueryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HelpCentreQueryAttachment>()
                .HasIndex(a => a.QueryId);

            /*
             =========================================
             OPERATOR NOTIFICATIONS
             =========================================
            */

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.Type, n.DedupeKey })
                .IsUnique()
                .HasFilter("[DedupeKey] IS NOT NULL");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.CreatedAt });

            modelBuilder.Entity<NotificationPreference>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationPreference>()
                .HasIndex(p => p.UserId)
                .IsUnique();
        }
    }
}