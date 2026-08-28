using System.Threading;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Helpers;
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

        public DbSet<RestaurantMembership> RestaurantMemberships { get; set; }

        public DbSet<RestaurantAccessActivity> RestaurantAccessActivities
        { get; set; }

        public DbSet<RestaurantAdminPermissionCell> RestaurantAdminPermissionCells
        { get; set; }

        public DbSet<TeamInvitation> TeamInvitations { get; set; }

        public DbSet<RestaurantBusinessDetails> RestaurantBusinessDetails
        { get; set; }

        public DbSet<BillingAccount> BillingAccounts { get; set; }

        public DbSet<CreditLedgerEntry> CreditLedgerEntries { get; set; }

        public DbSet<RestaurantBillingActivity> RestaurantBillingActivities
        { get; set; }

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

        public DbSet<FeedbackDetectedTagsChange> FeedbackDetectedTagsChanges { get; set; }

        public DbSet<FeedbackWorkflowStatusChange> FeedbackWorkflowStatusChanges { get; set; }

        public DbSet<FeedbackCloseOut> FeedbackCloseOuts { get; set; }

        public DbSet<FeedbackGuestResponse> FeedbackGuestResponses { get; set; }

        public DbSet<FeedbackInternalAction> FeedbackInternalActions { get; set; }

        public DbSet<FeedbackRecoveryOffer> FeedbackRecoveryOffers { get; set; }

        public DbSet<FeedbackRecoveryCompletion> FeedbackRecoveryCompletions { get; set; }

        public DbSet<Campaign> Campaigns { get; set; }

        public DbSet<CampaignFrozenRecipient> CampaignFrozenRecipients { get; set; }

        public DbSet<CampaignRecipientDelivery> CampaignRecipientDeliveries { get; set; }

        public DbSet<CatalogOffer> CatalogOffers { get; set; }

        public DbSet<OfferIssue> OfferIssues { get; set; }

        public DbSet<OfferRedeemFailedAttempt> OfferRedeemFailedAttempts { get; set; }

        public DbSet<OfferVoidRequest> OfferVoidRequests { get; set; }

        public DbSet<DataMigrationMarker> DataMigrationMarkers { get; set; }

        public DbSet<HelpCentreQuery> HelpCentreQueries { get; set; }

        public DbSet<HelpCentreQueryMessage> HelpCentreQueryMessages { get; set; }

        public DbSet<HelpCentreQueryAttachment> HelpCentreQueryAttachments { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<NotificationPreference> NotificationPreferences { get; set; }

        public DbSet<AssistantConversation> AssistantConversations { get; set; }

        public DbSet<AssistantMessage> AssistantMessages { get; set; }

        public DbSet<WeeklyBrief> WeeklyBriefs { get; set; }

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

            modelBuilder.Entity<RestaurantMembership>()
                .HasIndex(m => new { m.UserId, m.RestaurantId })
                .IsUnique();

            modelBuilder.Entity<RestaurantMembership>()
                .HasIndex(m => m.RestaurantId)
                .HasFilter("[PermissionRole] = N'Owner'")
                .IsUnique()
                .HasDatabaseName("IX_RestaurantMemberships_OneOwner");

            modelBuilder.Entity<RestaurantMembership>()
                .Property(m => m.PermissionRole)
                .HasMaxLength(40);

            modelBuilder.Entity<RestaurantMembership>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<RestaurantMembership>()
                .HasOne(m => m.Restaurant)
                .WithMany()
                .HasForeignKey(m => m.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RestaurantAccessActivity>()
                .HasOne(row => row.Restaurant)
                .WithMany()
                .HasForeignKey(row => row.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RestaurantAccessActivity>()
                .Property(row => row.Kind)
                .HasMaxLength(40);

            modelBuilder.Entity<RestaurantBillingActivity>()
                .HasOne(row => row.Restaurant)
                .WithMany()
                .HasForeignKey(row => row.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RestaurantBillingActivity>()
                .Property(row => row.Kind)
                .HasMaxLength(40);

            modelBuilder.Entity<RestaurantBillingActivity>()
                .HasIndex(row => new { row.RestaurantId, row.OccurredAtUtc, row.Id });

            modelBuilder.Entity<RestaurantAdminPermissionCell>()
                .HasOne(row => row.Restaurant)
                .WithMany()
                .HasForeignKey(row => row.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RestaurantAdminPermissionCell>()
                .HasIndex(row => new { row.RestaurantId, row.AreaId })
                .IsUnique();

            modelBuilder.Entity<RestaurantAdminPermissionCell>()
                .Property(row => row.AreaId)
                .HasMaxLength(40);

            modelBuilder.Entity<TeamInvitation>()
                .HasIndex(row => row.OpaqueReference)
                .IsUnique();

            modelBuilder.Entity<TeamInvitation>()
                .HasIndex(row => new { row.RestaurantId, row.Email })
                .IsUnique();

            modelBuilder.Entity<TeamInvitation>()
                .HasOne(row => row.Restaurant)
                .WithMany()
                .HasForeignKey(row => row.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeamInvitation>()
                .HasOne(row => row.InviterUser)
                .WithMany()
                .HasForeignKey(row => row.InviterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Restaurant>()
                .HasOne(r => r.BillingContactUser)
                .WithMany()
                .HasForeignKey(r => r.BillingContactUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Restaurant>()
                .HasOne(r => r.PrivacyContactUser)
                .WithMany()
                .HasForeignKey(r => r.PrivacyContactUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Restaurant>()
                .HasOne(r => r.SupportContactUser)
                .WithMany()
                .HasForeignKey(r => r.SupportContactUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Restaurant>()
                .HasOne(r => r.WorkspaceStatusChangedByUser)
                .WithMany()
                .HasForeignKey(r => r.WorkspaceStatusChangedByUserId)
                // NoAction: SQL Server rejects SET NULL alongside OwnerUser
                // CASCADE (error 1785, multiple cascade paths).
                .OnDelete(DeleteBehavior.NoAction);

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
             RESTAURANT -> BUSINESS DETAILS (1:1)
             =========================================
            */

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .HasOne(d => d.Restaurant)
                .WithOne(r => r.BusinessDetails)
                .HasForeignKey<RestaurantBusinessDetails>(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.LegalStructure)
                .HasMaxLength(32);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.LegalBusinessName)
                .HasMaxLength(200);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.TradingName)
                .HasMaxLength(200);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.CompanyNumber)
                .HasMaxLength(50);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.VatNumber)
                .HasMaxLength(50);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.CountryOfRegistration)
                .HasMaxLength(100);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.AddressLine1)
                .HasMaxLength(500);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.AddressLine2)
                .HasMaxLength(500);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.TownCity)
                .HasMaxLength(150);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.County)
                .HasMaxLength(150);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.Postcode)
                .HasMaxLength(20);

            modelBuilder.Entity<RestaurantBusinessDetails>()
                .Property(d => d.Country)
                .HasMaxLength(100);

            /*
             =========================================
             RESTAURANT -> BILLING ACCOUNT (1:1)
             =========================================
            */

            modelBuilder.Entity<BillingAccount>()
                .HasOne(b => b.Restaurant)
                .WithOne(r => r.BillingAccount)
                .HasForeignKey<BillingAccount>(b => b.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BillingAccount>()
                .Property(b => b.BillingEmail)
                .HasMaxLength(320);

            modelBuilder.Entity<BillingAccount>()
                .Property(b => b.RevolutCustomerId)
                .HasMaxLength(128);

            modelBuilder.Entity<BillingAccount>()
                .Property(b => b.SubscriptionPlan)
                .HasMaxLength(32)
                .IsRequired();

            modelBuilder.Entity<BillingAccount>()
                .Property(b => b.BillingCycle)
                .HasMaxLength(16);

            modelBuilder.Entity<BillingAccount>()
                .Property(b => b.BillingStatus)
                .HasMaxLength(32)
                .IsRequired();

            modelBuilder.Entity<BillingAccount>()
                .Property(b => b.ContractedPricebookId)
                .HasMaxLength(64)
                .IsRequired();

            modelBuilder.Entity<BillingAccount>()
                .Property(b => b.StarterKitState)
                .HasMaxLength(32)
                .IsRequired();

            modelBuilder.Entity<BillingAccount>()
                .HasIndex(b => b.RevolutCustomerId)
                .IsUnique()
                .HasFilter(
                    "[RevolutCustomerId] IS NOT NULL AND [RevolutCustomerId] <> ''"
                );

            modelBuilder.Entity<BillingAccount>()
                .Property(b => b.PaidExtraLocationCount)
                .HasDefaultValue(0);

            modelBuilder.Entity<BillingAccount>()
                .Property(b => b.DunningFiredSteps)
                .HasMaxLength(32);

            /*
             =========================================
             BILLING ACCOUNT -> CREDIT LEDGER ENTRIES
             =========================================
             */

            modelBuilder.Entity<CreditLedgerEntry>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_CreditLedgerEntries_QuantityPositive",
                    "[Quantity] > 0"
                ));

            modelBuilder.Entity<CreditLedgerEntry>()
                .HasOne(e => e.BillingAccount)
                .WithMany()
                .HasForeignKey(e => e.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict / NoAction: ledger LocationId must not add a second
            // cascade path onto Restaurant (SQL Server error 1785).
            modelBuilder.Entity<CreditLedgerEntry>()
                .HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CreditLedgerEntry>()
                .HasOne(e => e.Allocation)
                .WithMany()
                .HasForeignKey(e => e.AllocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CreditLedgerEntry>()
                .HasOne(e => e.ReversedEntry)
                .WithMany()
                .HasForeignKey(e => e.ReversedEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CreditLedgerEntry>()
                .HasIndex(e => new { e.RestaurantId, e.Channel });

            modelBuilder.Entity<CreditLedgerEntry>()
                .HasIndex(e => new { e.ReservationRef, e.AllocationId })
                .IsUnique()
                .HasFilter(
                    "[EntryType] = N'reservation' AND [ReservationRef] IS NOT NULL"
                );

            modelBuilder.Entity<CreditLedgerEntry>()
                .HasIndex(e => e.ReversedEntryId)
                .IsUnique()
                .HasFilter("[ReversedEntryId] IS NOT NULL");

            modelBuilder.Entity<CreditLedgerEntry>()
                .Property(e => e.Channel)
                .HasMaxLength(16)
                .IsRequired();

            modelBuilder.Entity<CreditLedgerEntry>()
                .Property(e => e.EntryType)
                .HasMaxLength(32)
                .IsRequired();

            modelBuilder.Entity<CreditLedgerEntry>()
                .Property(e => e.ReservationRef)
                .HasMaxLength(128);

            modelBuilder.Entity<CreditLedgerEntry>()
                .Property(e => e.PricebookVersion)
                .HasMaxLength(64);

            modelBuilder.Entity<CreditLedgerEntry>()
                .Property(e => e.Reason)
                .HasMaxLength(500);

            modelBuilder.Entity<CreditLedgerEntry>()
                .Property(e => e.SourcePaymentRef)
                .HasMaxLength(128);

            modelBuilder.Entity<CreditLedgerEntry>()
                .Property(e => e.CorrectionSource)
                .HasMaxLength(32);

            modelBuilder.Entity<CreditLedgerEntry>()
                .HasIndex(e => new { e.RestaurantId, e.SourcePaymentRef })
                .HasFilter("[SourcePaymentRef] IS NOT NULL");

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

            modelBuilder.Entity<LocationGuest>()
                .Property(lg => lg.MarketingPreference)
                .HasConversion(
                    v => v.ToWireString(),
                    v => LocationGuestMarketingPreferenceExtensions.FromWireString(v)
                )
                .HasMaxLength(32)
                .IsRequired();

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.LocationGuest)
                .WithMany(lg => lg.Feedbacks)
                .HasForeignKey(f => f.LocationGuestId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.RecoveryOffer)
                .WithMany()
                .HasForeignKey(f => f.RecoveryOfferId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<Feedback>()
                .HasIndex(f => f.RecoveryOfferId);

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
             FEEDBACK DETECTED TAGS CHANGES
             =========================================
            */

            modelBuilder.Entity<FeedbackDetectedTagsChange>()
                .HasOne(c => c.Feedback)
                .WithMany()
                .HasForeignKey(c => c.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackDetectedTagsChange>()
                .HasOne(c => c.AuthorUser)
                .WithMany()
                .HasForeignKey(c => c.AuthorUserId)
                // NoAction: SQL Server rejects AuthorUser SET NULL alongside
                // Feedback CASCADE (multiple cascade paths). Display name
                // is denormalized on the change row.
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackDetectedTagsChange>()
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
             FEEDBACK CLOSE-OUTS
             =========================================
            */

            modelBuilder.Entity<FeedbackCloseOut>()
                .HasOne(c => c.Feedback)
                .WithMany()
                .HasForeignKey(c => c.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackCloseOut>()
                .HasOne(c => c.WorkflowStatusChange)
                .WithMany()
                .HasForeignKey(c => c.WorkflowStatusChangeId)
                // Restrict: status-change row is audit; close-out owns the link.
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FeedbackCloseOut>()
                .HasOne(c => c.InternalNote)
                .WithMany()
                .HasForeignKey(c => c.InternalNoteId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackCloseOut>()
                .HasOne(c => c.AuthorUser)
                .WithMany()
                .HasForeignKey(c => c.AuthorUserId)
                // NoAction: SQL Server rejects AuthorUser SET NULL alongside
                // Feedback CASCADE (multiple cascade paths). Display name
                // is denormalized on the close-out row.
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackCloseOut>()
                .HasIndex(c => new { c.FeedbackId, c.CreatedAt });

            modelBuilder.Entity<FeedbackCloseOut>()
                .HasIndex(c => c.WorkflowStatusChangeId)
                .IsUnique();

            /*
             =========================================
             FEEDBACK GUEST RESPONSES
             =========================================
            */

            modelBuilder.Entity<FeedbackGuestResponse>()
                .HasOne(r => r.Feedback)
                .WithMany()
                .HasForeignKey(r => r.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackGuestResponse>()
                .HasOne(r => r.AuthorUser)
                .WithMany()
                .HasForeignKey(r => r.AuthorUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackGuestResponse>()
                .HasIndex(r => new { r.FeedbackId, r.CreatedAt });

            modelBuilder.Entity<FeedbackGuestResponse>()
                .HasIndex(r => new
                {
                    r.EmailDeliveryStatus,
                    r.EmailDeliveryRetryAfter,
                });

            /*
             =========================================
             FEEDBACK INTERNAL ACTIONS
             =========================================
            */

            modelBuilder.Entity<FeedbackInternalAction>()
                .HasOne(a => a.Feedback)
                .WithMany()
                .HasForeignKey(a => a.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackInternalAction>()
                .HasOne(a => a.AuthorUser)
                .WithMany()
                .HasForeignKey(a => a.AuthorUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackInternalAction>()
                .HasIndex(a => new { a.FeedbackId, a.CreatedAt });

            /*
             =========================================
             FEEDBACK RECOVERY OFFERS
             =========================================
            */

            modelBuilder.Entity<FeedbackRecoveryOffer>()
                .HasOne(o => o.Feedback)
                .WithMany()
                .HasForeignKey(o => o.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackRecoveryOffer>()
                .HasOne(o => o.GuestResponse)
                .WithMany()
                .HasForeignKey(o => o.GuestResponseId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackRecoveryOffer>()
                .HasOne(o => o.AuthorUser)
                .WithMany()
                .HasForeignKey(o => o.AuthorUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackRecoveryOffer>()
                .HasIndex(o => new { o.FeedbackId, o.CreatedAt });

            modelBuilder.Entity<FeedbackRecoveryOffer>()
                .HasIndex(o => o.RedemptionCode)
                .IsUnique();

            modelBuilder.Entity<FeedbackRecoveryOffer>()
                .Property(o => o.DiscountPercentage)
                .HasPrecision(8, 2);

            modelBuilder.Entity<FeedbackRecoveryOffer>()
                .Property(o => o.DiscountAmount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<FeedbackRecoveryOffer>()
                .Property(o => o.MinimumSpend)
                .HasPrecision(12, 2);

            /*
             =========================================
             FEEDBACK RECOVERY COMPLETIONS
             =========================================
            */

            modelBuilder.Entity<FeedbackRecoveryCompletion>()
                .HasOne(c => c.Feedback)
                .WithMany()
                .HasForeignKey(c => c.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackRecoveryCompletion>()
                .HasOne(c => c.WorkflowStatusChange)
                .WithMany()
                .HasForeignKey(c => c.WorkflowStatusChangeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FeedbackRecoveryCompletion>()
                .HasOne(c => c.AuthorUser)
                .WithMany()
                .HasForeignKey(c => c.AuthorUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<FeedbackRecoveryCompletion>()
                .HasIndex(c => new { c.FeedbackId, c.CreatedAt });

            modelBuilder.Entity<FeedbackRecoveryCompletion>()
                .HasIndex(c => c.WorkflowStatusChangeId)
                .IsUnique();

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
                .Property(q => q.AccountRequestKind)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToWireString() : null,
                    v => string.IsNullOrWhiteSpace(v)
                        ? null
                        : HelpCentreAccountRequestKindExtensions.FromWireString(v)
                );

            modelBuilder.Entity<HelpCentreQuery>()
                .HasOne(q => q.Restaurant)
                .WithMany()
                .HasForeignKey(q => q.RestaurantId)
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

            /*
             =========================================
             AI ASSISTANT CONVERSATIONS
             =========================================
            */

            modelBuilder.Entity<AssistantConversation>()
                .HasOne(c => c.OwnerUser)
                .WithMany()
                .HasForeignKey(c => c.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AssistantConversation>()
                .HasOne(c => c.OwnedLocation)
                .WithMany()
                .HasForeignKey(c => c.OwnedLocationId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<AssistantConversation>()
                .HasIndex(c => new { c.OwnerUserId, c.LastActivityAt });

            modelBuilder.Entity<AssistantMessage>()
                .Property(m => m.Role)
                .HasConversion(
                    v => v.ToWireString(),
                    v => AssistantMessageRoleExtensions.FromWireString(v)
                );

            modelBuilder.Entity<AssistantMessage>()
                .Property(m => m.Class)
                .HasConversion(
                    v => v == null ? null : v.Value.ToWireString(),
                    v => v == null
                        ? null
                        : AssistantMessageClassExtensions.FromWireString(v)
                );

            modelBuilder.Entity<AssistantMessage>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AssistantMessage>()
                .HasIndex(m => m.ConversationId);

            /*
             =========================================
             CAMPAIGN DRAFTS
             =========================================
             Restrict: hard-deleting a RestaurantLocation that still has
             Campaigns must fail with a clear FK constraint error rather than
             cascade-wiping Drafts (campaigns-audit/15).
            */

            modelBuilder.Entity<Campaign>()
                .HasOne(c => c.RestaurantLocation)
                .WithMany()
                .HasForeignKey(c => c.RestaurantLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Campaign>()
                .HasOne(c => c.Offer)
                .WithMany()
                .HasForeignKey(c => c.OfferId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Campaign>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Campaign>()
                .Property(c => c.RowVersion)
                .IsRowVersion();

            // Drafts list: filter location + status, sort UpdatedAt desc (audit 13 / DB-04).
            modelBuilder.Entity<Campaign>()
                .HasIndex(c => new
                {
                    c.RestaurantLocationId,
                    c.Status,
                    c.UpdatedAt,
                })
                .IsDescending(false, false, true);

            modelBuilder.Entity<CampaignFrozenRecipient>()
                .HasOne(row => row.Campaign)
                .WithMany()
                .HasForeignKey(row => row.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampaignFrozenRecipient>()
                .HasOne(row => row.LocationGuest)
                .WithMany()
                .HasForeignKey(row => row.LocationGuestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CampaignFrozenRecipient>()
                .HasIndex(row => new { row.CampaignId, row.LocationGuestId })
                .IsUnique();

            modelBuilder.Entity<CampaignRecipientDelivery>()
                .HasOne(row => row.Campaign)
                .WithMany()
                .HasForeignKey(row => row.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampaignRecipientDelivery>()
                .HasOne(row => row.LocationGuest)
                .WithMany()
                .HasForeignKey(row => row.LocationGuestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CampaignRecipientDelivery>()
                .HasIndex(row => new
                {
                    row.CampaignId,
                    row.LocationGuestId,
                    row.Channel,
                })
                .IsUnique();

            modelBuilder.Entity<CampaignRecipientDelivery>()
                .HasIndex(row => new { row.Outcome, row.AcceptedAtUtc });

            /*
             =========================================
             OFFERS CATALOG
             =========================================
            */

            modelBuilder.Entity<CatalogOffer>()
                .HasOne(o => o.RestaurantLocation)
                .WithMany()
                .HasForeignKey(o => o.RestaurantLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CatalogOffer>()
                .Property(o => o.DiscountPercentage)
                .HasPrecision(8, 2);

            modelBuilder.Entity<CatalogOffer>()
                .Property(o => o.DiscountAmount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<CatalogOffer>()
                .Property(o => o.MinimumSpend)
                .HasPrecision(12, 2);

            modelBuilder.Entity<CatalogOffer>()
                .HasIndex(o => new
                {
                    o.RestaurantLocationId,
                    o.Status,
                });

            modelBuilder.Entity<CatalogOffer>()
                .HasOne(o => o.CreatedByUser)
                .WithMany()
                .HasForeignKey(o => o.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CatalogOffer>()
                .Property(o => o.CreatedByDisplayName)
                .HasMaxLength(150);

            /*
             =========================================
             GUEST FORM THANK-YOU ATTACH (location → catalog)
             =========================================
             Restrict: deleting a CatalogOffer that is still attached as
             thank-you must fail rather than silently nulling the FK.
            */

            modelBuilder.Entity<RestaurantLocation>()
                .HasOne(l => l.ThankYouCatalogOffer)
                .WithMany()
                .HasForeignKey(l => l.ThankYouCatalogOfferId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RestaurantLocation>()
                .HasIndex(l => l.ThankYouCatalogOfferId);

            /*
             =========================================
             OFFER ISSUES (catalog pass + claim code)
             =========================================
            */

            modelBuilder.Entity<OfferIssue>()
                .HasOne(o => o.CatalogOffer)
                .WithMany()
                .HasForeignKey(o => o.CatalogOfferId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferIssue>()
                .HasOne(o => o.LocationGuest)
                .WithMany()
                .HasForeignKey(o => o.LocationGuestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferIssue>()
                .HasOne(o => o.Campaign)
                .WithMany()
                .HasForeignKey(o => o.CampaignId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<OfferIssue>()
                .HasOne(o => o.Feedback)
                .WithMany()
                .HasForeignKey(o => o.FeedbackId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<OfferIssue>()
                .HasIndex(o => o.ClaimCode)
                .IsUnique();

            modelBuilder.Entity<OfferIssue>()
                .HasIndex(o => new { o.CampaignId, o.LocationGuestId })
                .IsUnique()
                .HasFilter("[CampaignId] IS NOT NULL");

            modelBuilder.Entity<OfferIssue>()
                .Property(o => o.DiscountPercentage)
                .HasPrecision(8, 2);

            modelBuilder.Entity<OfferIssue>()
                .Property(o => o.DiscountAmount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<OfferIssue>()
                .Property(o => o.MinimumSpend)
                .HasPrecision(12, 2);

            /*
             =========================================
             OFFER REDEEM FAILED ATTEMPTS (metrics)
             =========================================
            */

            modelBuilder.Entity<OfferRedeemFailedAttempt>()
                .HasOne(a => a.CatalogOffer)
                .WithMany()
                .HasForeignKey(a => a.CatalogOfferId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferRedeemFailedAttempt>()
                .HasOne(a => a.RestaurantLocation)
                .WithMany()
                .HasForeignKey(a => a.RestaurantLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferRedeemFailedAttempt>()
                .HasIndex(a => new { a.CatalogOfferId, a.AttemptedAtUtc });

            /*
             =========================================
             OFFER VOID REQUESTS (ticket 39)
             =========================================
            */

            modelBuilder.Entity<OfferVoidRequest>()
                .HasOne(row => row.OfferIssue)
                .WithMany()
                .HasForeignKey(row => row.OfferIssueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferVoidRequest>()
                .HasOne(row => row.CatalogOffer)
                .WithMany()
                .HasForeignKey(row => row.CatalogOfferId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferVoidRequest>()
                .HasOne(row => row.RestaurantLocation)
                .WithMany()
                .HasForeignKey(row => row.RestaurantLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferVoidRequest>()
                .HasOne(row => row.RequestedByUser)
                .WithMany()
                .HasForeignKey(row => row.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OfferVoidRequest>()
                .HasOne(row => row.ResolvedByUser)
                .WithMany()
                .HasForeignKey(row => row.ResolvedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<OfferVoidRequest>()
                .HasIndex(row => row.CatalogOfferId);

            modelBuilder.Entity<OfferVoidRequest>()
                .HasIndex(row => row.RestaurantLocationId);

            modelBuilder.Entity<OfferVoidRequest>()
                .HasIndex(row => new { row.OfferIssueId, row.Status });

            /*
             =========================================
             WEEKLY BRIEF (home-weekly-brief / 01)
             =========================================
            */

            modelBuilder.Entity<WeeklyBrief>()
                .HasOne(row => row.Location)
                .WithMany()
                .HasForeignKey(row => row.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WeeklyBrief>()
                .HasIndex(row => new { row.LocationId, row.WeekKey })
                .IsUnique();

            modelBuilder.Entity<WeeklyBrief>()
                .Property(row => row.WeekKey)
                .HasMaxLength(32);

            modelBuilder.Entity<WeeklyBrief>()
                .Property(row => row.Status)
                .HasConversion(
                    v => v.ToWireString(),
                    v => WeeklyBriefStatusExtensions.FromWireString(v)
                )
                .HasMaxLength(32);
        }

        public override int SaveChanges()
        {
            EnsureRestaurantKeyContactDefaults();
            DropArchivedLocationIdsFromNamedLists();
            StampInMemoryCampaignRowVersions();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            EnsureRestaurantKeyContactDefaults();
            DropArchivedLocationIdsFromNamedLists();
            StampInMemoryCampaignRowVersions();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default
        )
        {
            EnsureRestaurantKeyContactDefaults();
            DropArchivedLocationIdsFromNamedLists();
            StampInMemoryCampaignRowVersions();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default
        )
        {
            EnsureRestaurantKeyContactDefaults();
            DropArchivedLocationIdsFromNamedLists();
            StampInMemoryCampaignRowVersions();
            return base.SaveChangesAsync(
                acceptAllChangesOnSuccess,
                cancellationToken
            );
        }

        /// <summary>
        /// Key contacts default to Account owner until set. Keeps Operator Setup
        /// and tests that only set OwnerUserId valid.
        /// </summary>
        private void EnsureRestaurantKeyContactDefaults()
        {
            foreach (var entry in ChangeTracker.Entries<Restaurant>())
            {
                if (
                    entry.State != EntityState.Added
                    && entry.State != EntityState.Modified
                )
                {
                    continue;
                }

                var restaurant = entry.Entity;
                if (restaurant.OwnerUserId == 0)
                {
                    continue;
                }

                if (restaurant.BillingContactUserId == 0)
                {
                    restaurant.BillingContactUserId = restaurant.OwnerUserId;
                }

                if (restaurant.PrivacyContactUserId == 0)
                {
                    restaurant.PrivacyContactUserId = restaurant.OwnerUserId;
                }

                if (restaurant.SupportContactUserId == 0)
                {
                    restaurant.SupportContactUserId = restaurant.OwnerUserId;
                }
            }
        }

        private void DropArchivedLocationIdsFromNamedLists()
        {
            var removed = ChangeTracker
                .Entries<RestaurantLocation>()
                .Where(e => e.State == EntityState.Deleted)
                .Select(e => e.Entity)
                .ToList();

            if (removed.Count == 0)
            {
                return;
            }

            var restaurantIds = removed
                .Select(l => l.RestaurantId)
                .Distinct()
                .ToList();

            var memberships = RestaurantMemberships
                .Where(m => restaurantIds.Contains(m.RestaurantId))
                .ToList();

            foreach (var location in removed)
            {
                foreach (var membership in memberships)
                {
                    if (
                        membership.RestaurantId != location.RestaurantId
                        || membership.LocationScope != LocationScopeKind.NamedList
                    )
                    {
                        continue;
                    }

                    var ids = MembershipLocationScope
                        .ParseNamedIds(membership.NamedLocationIdsJson)
                        .Where(id => id != location.Id)
                        .ToList();

                    membership.NamedLocationIdsJson =
                        MembershipLocationScope.SerializeNamedIds(ids);
                }
            }
        }

        /// <summary>
        /// SQL Server owns rowversion generation. InMemory does not, so tests stamp
        /// an opaque 8-byte token on each Campaign insert/update.
        /// </summary>
        private void StampInMemoryCampaignRowVersions()
        {
            if (
                !string.Equals(
                    Database.ProviderName,
                    "Microsoft.EntityFrameworkCore.InMemory",
                    StringComparison.Ordinal
                )
            )
            {
                return;
            }

            foreach (var entry in ChangeTracker.Entries<Campaign>())
            {
                if (
                    entry.State != EntityState.Added
                    && entry.State != EntityState.Modified
                )
                {
                    continue;
                }

                entry.Property(c => c.RowVersion).CurrentValue =
                    BitConverter.GetBytes(Interlocked.Increment(ref _inMemoryRowVersion));
            }
        }

        private static long _inMemoryRowVersion;
    }
}