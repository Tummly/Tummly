using System.ComponentModel.DataAnnotations;
namespace TummlyBackend.DTOs.Auth
{
    public class AdminLoginDto
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; } 

        [Required]
        public string? Password { get; set; } 
    }
}