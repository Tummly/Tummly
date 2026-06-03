using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.DTOs.Auth
{
    public class AdminLoginDto
    {
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
         PASSWORD
         =========================================
        */

        [Required]
        public string Password { get; set; }
    }
}