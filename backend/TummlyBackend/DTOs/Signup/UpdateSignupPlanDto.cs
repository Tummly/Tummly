namespace TummlyBackend.DTOs.Signup
{
    /// <summary>
    /// The quote a guest reviewed again after a paid checkout did not open.
    /// Same plan fields as onboarding.
    /// </summary>
    public class UpdateSignupPlanDto
    {
        /// <summary>Starter|Growth|Group|Custom</summary>
        public string? ChosenPlan { get; set; }

        /// <summary>monthly|annual</summary>
        public string? ChosenCadence { get; set; }

        /// <summary>JSON (or base64 JSON) config with the reviewed quote hash.</summary>
        public string? CustomPlanConfigJson { get; set; }
    }
}
