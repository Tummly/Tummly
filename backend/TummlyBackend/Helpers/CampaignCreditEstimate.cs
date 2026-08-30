namespace TummlyBackend.Helpers
{
    public static class CampaignCreditEstimate
    {
        public static int EstimateUnits(
            string channel,
            string? messageBody,
            int recipientCount
        )
        {
            if (recipientCount <= 0)
            {
                return 0;
            }

            var normalized = (channel ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "email")
            {
                return recipientCount;
            }

            if (normalized == "sms")
            {
                return CampaignSmsSegmentCalculator.CountSegments(messageBody)
                    * recipientCount;
            }

            return recipientCount;
        }
    }
}
