using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class TrialRequest
    {
        public int Id { get; set; }

        /*
         =========================================
         BUSINESS NAME
         =========================================
        */

        [Required]
        [MaxLength(200)]
        public string BusinessName { get; set; }
            = string.Empty;

        /*
         =========================================
         BUSINESS CATEGORY
         =========================================
        */

        [Required]
        [MaxLength(150)]
        public string BusinessCategory { get; set; }
            = string.Empty;

        /*
         =========================================
         LOCATIONS
         =========================================
        */

        [Required]
        [MaxLength(50)]
        public string Locations { get; set; }
            = string.Empty;

        /*
         =========================================
         BUSINESS LINK
         =========================================
        */

        [MaxLength(500)]
        public string? BusinessLink { get; set; }

        /*
         =========================================
         FULL NAME
         =========================================
        */

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; }
            = string.Empty;

        /*
         =========================================
         EMAIL
         =========================================
        */

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; }
            = string.Empty;

        /*
         =========================================
         MOBILE
         =========================================
        */

        [Required]
        [MaxLength(50)]
        public string Mobile { get; set; }
            = string.Empty;

        /*
         =========================================
         ROLE
         =========================================
        */

        [Required]
        [MaxLength(100)]
        public string Role { get; set; }
            = string.Empty;

        /*
         =========================================
         GOAL
         =========================================
        */

        [Required]
        [MaxLength(500)]
        public string Goal { get; set; }
            = string.Empty;

        /*
         =========================================
         TERMS
         =========================================
        */

        public bool TermsAccepted { get; set; }

        /*
         =========================================
         EMAIL VERIFIED
         =========================================
        */

        public bool IsEmailVerified { get; set; }
            = false;

        /*
         =========================================
         ADMIN APPROVAL
         =========================================
        */

        public bool IsApproved { get; set; }
            = false;

        /*
         =========================================
         STATUS
         =========================================
        */

        public string Status { get; set; }
            = "EMAIL_VERIFIED";

        /*
         =========================================
         APPROVED DATE
         =========================================
        */

        public DateTime? ApprovedAt { get; set; }

        /*
         =========================================
         APPROVAL TOKEN
         =========================================
        */

        [MaxLength(300)]
        public string? ApprovalToken { get; set; }

        /*
         =========================================
         INVITE EXPIRY
         =========================================
        */

        public DateTime? InviteExpiresAt { get; set; }


        /*
 =========================================
 REVIEWED BY
 =========================================
*/

        [MaxLength(200)]
        public string? ReviewedBy { get; set; }

        /*
         =========================================
         REVIEWED DATE
         =========================================
        */

        public DateTime? ReviewedAt { get; set; }

        /*
         =========================================
         ADMIN NOTES
         =========================================
        */

        [MaxLength(2000)]
        public string? AdminNotes { get; set; }

        /*
         =========================================
         MORE INFO REQUESTED
         =========================================
        */

        public DateTime? MoreInfoRequestedAt { get; set; }

        /*
         =========================================
         MORE INFO MESSAGE
         =========================================
        */

        [MaxLength(2000)]
        public string? MoreInfoMessage { get; set; }

        /*
         =========================================
         DECLINED DATE
         =========================================
        */

        public DateTime? DeclinedAt { get; set; }

        /*
         =========================================
         DECLINE REASON
         =========================================
        */

        [MaxLength(2000)]
        public string? DeclineReason { get; set; }

        /*
         =========================================
         INVITE SENT DATE
         =========================================
        */

        public DateTime? InviteSentAt { get; set; }

        /*
         =========================================
         ACCOUNT CREATED DATE
         =========================================
        */

        public DateTime? AccountCreatedAt { get; set; }

        /*
         =========================================
         ACCOUNT CREATED
         =========================================
        */

        public bool IsAccountCreated { get; set; }
            = false;

        /*
         =========================================
         ACCOUNT TYPE
         =========================================
        */

        public string AccountType { get; set; }
            = "Single";

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}