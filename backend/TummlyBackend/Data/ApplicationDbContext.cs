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

        public DbSet<QrCode> QrCodes { get; set; }

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

        public DbSet<QrScanEvent> QrScanEvents { get; set; }

        public DbSet<LocationGuestNote> LocationGuestNotes { get; set; }

        public DbSet<FeedbackInternalNote> FeedbackInternalNotes { get; set; }

        public DbSet<FeedbackClassificationCorrection> FeedbackClassificationCorrections { get; set; }

        public DbSet<FeedbackWorkflowStatusChange> FeedbackWorkflowStatusChanges { get; set; }

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
             QR CODES (per-location QR type / QR link)
             =========================================
             */

            modelBuilder.Entity<QrCode>()
                .HasOne(q => q.RestaurantLocation)
                .WithMany()
                .HasForeignKey(q => q.RestaurantLocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QrCode>()
                .HasIndex(q => q.Token)
                .IsUnique();

            // Filtered unique: at most one Active/Paused QR code per
            // (location, type) for catalog four + Smart Guest. Digital guest
            // link (QrType = 5) is excluded so many may exist per location.
            // Status ints: Active = 0, Paused = 1.
            modelBuilder.Entity<QrCode>()
                .HasIndex(q => new { q.RestaurantLocationId, q.QrType })
                .IsUnique()
                .HasFilter("[Status] IN (0, 1) AND [QrType] <> 5");

            // Digital guest links: case-insensitive Link name uniqueness among
            // non-archived rows at the location.
            modelBuilder.Entity<QrCode>()
                .HasIndex(q => new { q.RestaurantLocationId, q.NormalizedLinkName })
                .IsUnique()
                .HasFilter(
                    "[QrType] = 5 AND [Status] IN (0, 1) AND [NormalizedLinkName] IS NOT NULL"
                );

            // Capture Archive list: owned-location archived lookup by ArchivedAt.
            modelBuilder.Entity<QrCode>()
                .HasIndex(q => new
                {
                    q.RestaurantLocationId,
                    q.Status,
                    q.ArchivedAt,
                });

            modelBuilder.Entity<QrCode>()
                .Property(q => q.LinkName)
                .HasMaxLength(100);

            modelBuilder.Entity<QrCode>()
                .Property(q => q.NormalizedLinkName)
                .HasMaxLength(100);

            modelBuilder.Entity<QrCode>()
                .Property(q => q.InternalDescription)
                .HasMaxLength(500);

            modelBuilder.Entity<QrCode>()
                .Property(q => q.ArchivedByDisplayName)
                .HasMaxLength(150);

            modelBuilder.Entity<QrCode>()
                .Property(q => q.CreatedByDisplayName)
                .HasMaxLength(150);

            modelBuilder.Entity<QrCode>()
                .Property(q => q.UpdatedByDisplayName)
                .HasMaxLength(150);

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

            // Restrict: a QR code with Feedback attached cannot be hard-deleted.
            // Pausing/archiving the QR code does not affect existing Feedback.
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.QrCode)
                .WithMany()
                .HasForeignKey(f => f.QrCodeId)
                .OnDelete(DeleteBehavior.Restrict);

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
             QR SCAN EVENTS
             =========================================
            */

            modelBuilder.Entity<QrScanEvent>()
                .HasOne(e => e.RestaurantLocation)
                .WithMany()
                .HasForeignKey(e => e.RestaurantLocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QrScanEvent>()
                .HasOne(e => e.QrCode)
                .WithMany()
                .HasForeignKey(e => e.QrCodeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<QrScanEvent>()
                .HasIndex(e => new { e.RestaurantLocationId, e.CreatedAt });

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
                // NoAction: SQL Server rejects AuthorUser SET NULL alongside
                // LocationGuest CASCADE (multiple cascade paths). Display name
                // is denormalized on the note row.
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<LocationGuestNote>()
                .HasOne(n => n.LastEditedByUser)
                .WithMany()
                .HasForeignKey(n => n.LastEditedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<LocationGuestNote>()
                .HasOne(n => n.DeletedByUser)
                .WithMany()
                .HasForeignKey(n => n.DeletedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<LocationGuestNote>()
                .HasIndex(n => new { n.LocationGuestId, n.CreatedAt });

            /*
             =========================================
             FEEDBACK INTERNAL NOTES
             =========================================
            */

            modelBuilder.Entity<FeedbackInternalNote>()
                .HasOne(n => n.Feedback)
                .WithMany()
                .HasForeignKey(n => n.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackInternalNote>()
                .HasOne(n => n.AuthorUser)
                .WithMany()
                .HasForeignKey(n => n.AuthorUserId)
                // NoAction: SQL Server rejects AuthorUser SET NULL alongside
                // Feedback CASCADE (multiple cascade paths). Display name
                // is denormalized on the note row.
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackInternalNote>()
                .HasOne(n => n.LastEditedByUser)
                .WithMany()
                .HasForeignKey(n => n.LastEditedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackInternalNote>()
                .HasOne(n => n.DeletedByUser)
                .WithMany()
                .HasForeignKey(n => n.DeletedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackInternalNote>()
                .HasIndex(n => new { n.FeedbackId, n.CreatedAt });

            /*
             =========================================
             FEEDBACK CLASSIFICATION CORRECTIONS
             =========================================
            */

            modelBuilder.Entity<FeedbackClassificationCorrection>()
                .HasOne(c => c.Feedback)
                .WithMany()
                .HasForeignKey(c => c.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackClassificationCorrection>()
                .HasOne(c => c.AuthorUser)
                .WithMany()
                .HasForeignKey(c => c.AuthorUserId)
                // NoAction: SQL Server rejects AuthorUser SET NULL alongside
                // Feedback CASCADE (multiple cascade paths). Display name
                // is denormalized on the correction row.
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackClassificationCorrection>()
                .HasIndex(c => new { c.FeedbackId, c.CreatedAt });

            /*
             =========================================
             FEEDBACK WORKFLOW STATUS CHANGES
             =========================================
            */

            modelBuilder.Entity<FeedbackWorkflowStatusChange>()
                .HasOne(c => c.Feedback)
                .WithMany()
                .HasForeignKey(c => c.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackWorkflowStatusChange>()
                .HasOne(c => c.AuthorUser)
                .WithMany()
                .HasForeignKey(c => c.AuthorUserId)
                // NoAction: SQL Server rejects AuthorUser SET NULL alongside
                // Feedback CASCADE (multiple cascade paths). Display name
                // is denormalized on the change row.
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackWorkflowStatusChange>()
                .HasIndex(c => new { c.FeedbackId, c.CreatedAt });

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