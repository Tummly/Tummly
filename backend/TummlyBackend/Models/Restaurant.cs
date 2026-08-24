using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        /*
         =========================================
         RESTAURANT NAME
         =========================================
        */

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
            = string.Empty;

        /*
         =========================================
         ACCOUNT TYPE
         Single = 1 location
         Multi = 2-5 OR 6+
         =========================================
        */

        public string AccountType { get; set; }
            = "Single";

        /*
         =========================================
         OWNER USER
         =========================================
        */

        public int OwnerUserId { get; set; }

        public User OwnerUser { get; set; }

        /*
         =========================================
         KEY CONTACTS
         Nominations only — not RBAC. Defaults to OwnerUserId.
         When Team & permissions later removes a member who holds a
         writable contact, reassign that role to OwnerUserId first.
         =========================================
        */

        public int BillingContactUserId { get; set; }

        public User BillingContactUser { get; set; } = null!;

        public int PrivacyContactUserId { get; set; }

        public User PrivacyContactUser { get; set; } = null!;

        public int SupportContactUserId { get; set; }

        public User SupportContactUser { get; set; } = null!;

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        /*
         =========================================
         LOCATIONS
         =========================================
        */

        public ICollection<RestaurantLocation>
            Locations
        { get; set; }


            = new List<RestaurantLocation>();
        public GuestLoopSetup? GuestLoopSetup { get; set; }

        public string? BusinessCategory { get; set; }

        public string? BusinessLink { get; set; }

        public string? PublicPhoneNumber { get; set; }

        /*
         =========================================
         ACCOUNT & WORKSPACE
         =========================================
        */

        public WorkspaceStatus WorkspaceStatus { get; set; }
            = WorkspaceStatus.Active;

        public DateTime? WorkspaceStatusChangedAt { get; set; }

        public int? WorkspaceStatusChangedByUserId { get; set; }

        public User? WorkspaceStatusChangedByUser { get; set; }

        [MaxLength(500)]
        public string? BrandLogoObjectKey { get; set; }

        [MaxLength(100)]
        public string? BrandLogoContentType { get; set; }

        public DateTime? AccountWorkspaceLastSavedAt { get; set; }

        public RestaurantBusinessDetails? BusinessDetails { get; set; }

        /*
         =========================================
         WORKSPACE DEFAULTS
         Restaurant-wide. Missing/invalid → product fallbacks.
         =========================================
        */

        [MaxLength(16)]
        public string? WeekStartsOn { get; set; }

        [MaxLength(16)]
        public string? DefaultReportingPeriod { get; set; }

        [MaxLength(200)]
        public string? DefaultCampaignSenderName { get; set; }

    }



}