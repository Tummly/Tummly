namespace TummlyBackend.Billing
{
    public sealed class ActiveOfferCapDecision
    {
        public bool AllowIncrement { get; init; }

        public bool Unavailable { get; init; }

        public int Cap { get; init; }

        public int Current { get; init; }

        public static ActiveOfferCapDecision Allow(int cap, int current) => new()
        {
            AllowIncrement = true,
            Cap = cap,
            Current = current,
        };

        public static ActiveOfferCapDecision AtCap(int cap, int current) => new()
        {
            AllowIncrement = false,
            Cap = cap,
            Current = current,
        };

        public static ActiveOfferCapDecision UnavailableNow() => new()
        {
            AllowIncrement = false,
            Unavailable = true,
        };
    }
}
