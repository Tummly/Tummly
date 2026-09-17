namespace TummlyBackend.DTOs.Signup
{
    public class StartSignupDto
    {
        public string Email { get; set; } = string.Empty;

        public bool TermsAccepted { get; set; }
    }
}
