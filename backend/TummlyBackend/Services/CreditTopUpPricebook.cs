namespace TummlyBackend.Services
{
    public sealed record CreditTopUpPack(string Channel, int Quantity, decimal NetPounds);

    public static class CreditTopUpPricebook
    {
        private const decimal VatRate = 0.20m;

        private static readonly CreditTopUpPack[] Packs =
        [
            new("sms", 100, 12m),
            new("sms", 500, 55m),
            new("sms", 1000, 100m),
            new("sms", 5000, 450m),
            new("ai", 100, 5m),
            new("ai", 500, 15m),
            new("ai", 2000, 39m),
            new("email", 5000, 10m),
            new("email", 20000, 30m),
            new("email", 50000, 60m),
        ];

        public static CreditTopUpPack? FindPack(string channel, int quantity)
        {
            var normalized = channel.Trim().ToLowerInvariant();
            return Packs.FirstOrDefault(pack =>
                pack.Channel == normalized && pack.Quantity == quantity
            );
        }

        public static bool IsSms5000Allowed(string subscriptionPlan, bool billingAccountAllows)
        {
            if (string.Equals(subscriptionPlan, "Group", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return billingAccountAllows;
        }

        public static bool IsPackVisible(
            CreditTopUpPack pack,
            string subscriptionPlan,
            bool billingAccountAllows
        )
        {
            if (pack.Channel == "sms" && pack.Quantity == 5000)
            {
                return IsSms5000Allowed(subscriptionPlan, billingAccountAllows);
            }

            return true;
        }

        public static decimal GrossPounds(decimal netPounds)
        {
            return Math.Round(netPounds * (1m + VatRate), 2, MidpointRounding.AwayFromZero);
        }

        public static string FormatPounds(decimal amount)
        {
            return $"£{amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}";
        }
    }
}
