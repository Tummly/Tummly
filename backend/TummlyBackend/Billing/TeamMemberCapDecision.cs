namespace TummlyBackend.Billing
{
    public sealed class TeamMemberCapDecision
    {
        public bool AllowIncrement { get; init; }

        public bool Unavailable { get; init; }

        public int Cap { get; init; }

        public int Current { get; init; }

        /// <summary>Set when a scheduled downgrade sets the cap.</summary>
        public string? Message { get; init; }

        public static TeamMemberCapDecision Allow(int cap, int current) => new()
        {
            AllowIncrement = true,
            Cap = cap,
            Current = current,
        };

        public static TeamMemberCapDecision AtCap(
            int cap,
            int current,
            string? message = null
        ) => new()
        {
            AllowIncrement = false,
            Cap = cap,
            Current = current,
            Message = message,
        };

        public static TeamMemberCapDecision UnavailableNow() => new()
        {
            AllowIncrement = false,
            Unavailable = true,
        };
    }
}
