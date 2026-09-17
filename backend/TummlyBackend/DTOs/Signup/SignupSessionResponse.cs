namespace TummlyBackend.DTOs.Signup
{
    public class SignupSessionResponse
    {
        public Guid SessionToken { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
