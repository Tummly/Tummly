namespace TummlyBackend.Configurations
{
    /// <summary>
    /// Public product-demo walkthrough: issue a JWT for a fixed seeded Single owner.
    /// </summary>
    public class DemoSessionOptions
    {
        public const string SectionName = "DemoSession";

        /// <summary>When false, POST /api/auth/demo-session returns 503.</summary>
        public bool Enabled { get; set; }

        /// <summary>Owner email of the seeded Pilot Single restaurant.</summary>
        public string OwnerEmail { get; set; } = string.Empty;
    }
}
