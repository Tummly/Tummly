namespace TummlyBackend.DTOs.Signup
{
    public class SignupProvisioningStatusResponse
    {
        public string Status { get; set; } = string.Empty;

        public bool Ready { get; set; }
    }
}
