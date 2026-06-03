using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class PendingTrialRequest
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
         OTP RESEND COUNT
         =========================================
        */

        public int OtpResendCount { get; set; }
            = 0;

        /*
         =========================================
         LAST OTP SENT TIME
         =========================================
        */

        public DateTime? LastOtpSentAt { get; set; } = null;

        /*
         =========================================
         IS ABANDONED
         =========================================
        */

        public bool IsAbandoned { get; set; }
            = false;

        /*
         =========================================
         STATUS
         =========================================
        */

        public string Status { get; set; }
            = "Email Pending";

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}