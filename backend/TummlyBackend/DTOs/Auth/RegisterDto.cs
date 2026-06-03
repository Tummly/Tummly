namespace TummlyBackend.DTOs.Auth
{
    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;

        public string BusinessName { get; set; } = string.Empty;

        public string AccountType { get; set; } = string.Empty;
    }
}