using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class User
    {
        public int Id { get; set; }

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
         PASSWORD HASH
         =========================================
        */

        [Required]
        public string PasswordHash { get; set; }
            = string.Empty;

        /*
         =========================================
         PHONE NUMBER
         =========================================
        */

        [Required]
        public string PhoneNumber { get; set; }
            = string.Empty;

        /*
         =========================================
         ROLE
         =========================================
        */

        public string Role { get; set; }
            = "Owner";

        /*
         =========================================
         ACCOUNT TYPE
         =========================================
         Single = 1 location
         Multi = 2-5 OR 6+
        */

        public string AccountType { get; set; }
            = "Single";

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

        public bool IsApprovedByAdmin { get; set; }
            = false;

        /*
         =========================================
         ACCOUNT LOCK
         =========================================
        */

        public bool IsLocked { get; set; }
            = false;

        /*
         =========================================
         FAILED LOGIN ATTEMPTS
         =========================================
        */

        public int FailedLoginAttempts { get; set; }
            = 0;

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        /*
         =========================================
         OWNED RESTAURANTS
         =========================================
        */

        public ICollection<Restaurant>
            OwnedRestaurants
        { get; set; }
            = new List<Restaurant>();

        /*
         =========================================
         REFRESH TOKENS
         =========================================
        */

        public ICollection<RefreshToken>
            RefreshTokens
        { get; set; }
            = new List<RefreshToken>();

        public bool TermsAccepted { get; set; }
    }
}