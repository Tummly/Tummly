namespace TummlyBackend.DTOs.Signup
{
    public class SignupProvisioningStatusResponse
    {
        public string Status { get; set; } = string.Empty;

        public bool Ready { get; set; }

        /// <summary>
        /// Open Revolut checkout for paid Pricing signup while billing stays Free.
        /// </summary>
        public string? PaymentRedirectUrl { get; set; }
    }
}
