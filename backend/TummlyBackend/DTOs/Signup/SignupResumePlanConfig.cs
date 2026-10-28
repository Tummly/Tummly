namespace TummlyBackend.DTOs.Signup
{
    /// <summary>
    /// Saved Custom plan calculator config returned on resume. Mirrors the stored
    /// CustomPlanConfig v2 fields the guest sees again on review.
    /// </summary>
    public class SignupResumePlanConfig
    {
        /// <summary>starter | growth | group</summary>
        public string BasePlan { get; set; } = string.Empty;

        /// <summary>Configured Locations (1..500).</summary>
        public int Locations { get; set; }

        /// <summary>Extras: pack key → pack count.</summary>
        public Dictionary<string, int> Packs { get; set; } = [];

        /// <summary>Fingerprint of the quote the guest confirmed. Review compares it with a fresh quote.</summary>
        public string? QuoteHash { get; set; }
    }
}
