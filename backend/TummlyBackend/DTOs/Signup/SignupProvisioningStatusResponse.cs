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

        /// <summary>Set with status PaymentFailed (e.g. revolut_http_error).</summary>
        public string? PaymentFailureCode { get; set; }

        /// <summary>
        /// With a failure that needs a new quote review (quote_changed, legacy_pack_key):
        /// the plan and config the guest saved. Null otherwise.
        /// </summary>
        public SignupSavedPlanDto? SavedPlan { get; set; }
    }

    public class SignupSavedPlanDto
    {
        public string? ChosenPlan { get; set; }

        public string? ChosenCadence { get; set; }

        public string? CustomPlanConfigJson { get; set; }
    }
}
