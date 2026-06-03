using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class Admin
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

        /*
         =========================================
         EMAIL
         =========================================
        */

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /*
         =========================================
         PASSWORD HASH
         =========================================
        */

        [Required]
        public string PasswordHash { get; set; }

        /*
         =========================================
         ROLE
         =========================================
        */

        [Required]
        public string Role { get; set; } = "Admin";

        /*
         =========================================
         ACTIVE STATUS
         =========================================
        */

        public bool IsActive { get; set; } = true;

        /*
         =========================================
         FAILED LOGIN ATTEMPTS
         =========================================
        */

        public int FailedLoginAttempts { get; set; } = 0;

        /*
         =========================================
         LOCKOUT STATUS
         =========================================
        */

        public bool IsLocked { get; set; } = false;

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}