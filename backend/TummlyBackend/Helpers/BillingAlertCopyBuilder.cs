namespace TummlyBackend.Helpers
{
    public sealed record BillingAlertCopy(string Title, string Body);

    /// <summary>
    /// Same title and body for Email and in-app Account notices.
    /// Body includes workspace name; channel labels follow lock 03.
    /// </summary>
    public static class BillingAlertCopyBuilder
    {
        public static BillingAlertCopy CreditThreshold(
            string workspaceName,
            string channel,
            int thresholdBand
        )
        {
            var channelLabel = BillingAlertChannelLabels.LabelFor(channel);
            var safeWorkspace = workspaceName.Trim();

            return thresholdBand switch
            {
                80 => new BillingAlertCopy(
                    $"{channelLabel} are 80% used",
                    $"{safeWorkspace}: {channelLabel} are 80% used this cycle. Top up or reduce usage to avoid running out."
                ),
                90 => new BillingAlertCopy(
                    $"{channelLabel} are 90% used",
                    $"{safeWorkspace}: {channelLabel} are 90% used this cycle. Top up soon to keep campaigns and messages running."
                ),
                100 => new BillingAlertCopy(
                    $"No {channelLabel} remaining",
                    $"{safeWorkspace}: You have used all {channelLabel} for this period. Buy more credits or change plan to continue sending."
                ),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(thresholdBand),
                    thresholdBand,
                    "Credit threshold must be 80, 90, or 100."
                ),
            };
        }

        public static BillingAlertCopy PaymentFailureDay(
            string workspaceName,
            int dayStep
        )
        {
            var safeWorkspace = workspaceName.Trim();

            return dayStep switch
            {
                0 => new BillingAlertCopy(
                    "Payment failed",
                    $"{safeWorkspace}: We could not charge your payment method. Update it to keep your subscription active."
                ),
                3 => new BillingAlertCopy(
                    "Payment still overdue",
                    $"{safeWorkspace}: Your payment is still overdue. Update your payment method to avoid service interruption."
                ),
                7 => new BillingAlertCopy(
                    "Payment overdue — sends blocked",
                    $"{safeWorkspace}: Your payment is 7 days overdue. New Campaign and recovery sends are blocked until you update your payment method."
                ),
                10 => new BillingAlertCopy(
                    "Account locked — payment overdue",
                    $"{safeWorkspace}: Your account is locked because payment is overdue. Update your payment method to restore access."
                ),
                24 => new BillingAlertCopy(
                    "Account locked — payment overdue",
                    $"{safeWorkspace}: Your account remains locked. Update your payment method to restore access."
                ),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(dayStep),
                    dayStep,
                    "Dunning day step must be 0, 3, 7, 10, or 24."
                ),
            };
        }

        public static BillingAlertCopy UnpaidPilotLock(string workspaceName)
        {
            var safeWorkspace = workspaceName.Trim();
            return new BillingAlertCopy(
                "Pilot period ended",
                $"{safeWorkspace}: Your Pilot period has ended. Choose a paid plan to keep using Tummly."
            );
        }

        public static string NotificationTypeForCreditThreshold(int thresholdBand)
        {
            return thresholdBand switch
            {
                80 => "credit-warning-80",
                90 => "credit-warning-90",
                100 => "credit-warning-100",
                _ => throw new ArgumentOutOfRangeException(nameof(thresholdBand)),
            };
        }

        public static string NotificationTypeForPaymentFailureDay(int dayStep)
        {
            return dayStep switch
            {
                0 => "payment-failure-day-0",
                3 => "payment-failure-day-3",
                7 => "payment-failure-day-7",
                10 => "payment-failure-day-10",
                24 => "payment-failure-day-24",
                _ => throw new ArgumentOutOfRangeException(nameof(dayStep)),
            };
        }

        public const string UnpaidPilotLockNotificationType = "unpaid-pilot-lock";
    }
}
