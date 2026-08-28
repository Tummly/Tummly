using System.Globalization;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class BillingActivityCopyFormatter
    {
        public static string FormatSentence(RestaurantBillingActivity row)
        {
            var actor = row.ActorDisplayName ?? "";
            var channel = FormatChannelCredits(row.Channel, row.Qty);
            var plan = row.Plan ?? "";
            var cadence = row.Cadence ?? "";
            var planAndOrCadence = FormatPlanAndOrCadence(plan, cadence);
            var date = row.ScheduledDateLabel ?? "";

            return row.Kind switch
            {
                BillingActivityKinds.SubscriptionCreated =>
                    $"{actor} started {plan} ({cadence}).",
                BillingActivityKinds.SubscriptionUpgraded =>
                    $"{actor} upgraded to {plan}.",
                BillingActivityKinds.SubscriptionChangeScheduled =>
                    $"{actor} scheduled a change to {planAndOrCadence} on {date}.",
                BillingActivityKinds.AdditionalLocationAdded =>
                    $"{actor} added an Additional Group Location ({row.LocationName ?? ""}).",
                BillingActivityKinds.AdditionalLocationRemoveScheduled =>
                    $"{actor} scheduled removal of an Additional Group Location ({row.LocationName ?? ""}) on {date}.",
                BillingActivityKinds.SubscriptionCancelled =>
                    $"{actor} scheduled Cancel plan on {date}.",
                BillingActivityKinds.SubscriptionRenewed =>
                    $"{plan} renewed.",
                BillingActivityKinds.TopupPurchased =>
                    $"{channel} added by {actor}.",
                BillingActivityKinds.TopupRefunded =>
                    $"{channel} refunded.",
                BillingActivityKinds.CreditConsumed when row.ConsumeSource == "feedback_recovery" =>
                    $"{FormatChannelCredits(CreditChannels.Sms, row.Qty ?? 1)} used by Feedback recovery.",
                BillingActivityKinds.CreditConsumed =>
                    $"{channel} used by {row.CampaignName ?? ""}.",
                BillingActivityKinds.CreditExpired =>
                    FormatCreditExpired(row.Channel, row.Qty),
                BillingActivityKinds.ManualCreditAdjusted =>
                    FormatManualAdjust(row.Channel, row.Qty, row.ManualAdjustDirection),
                BillingActivityKinds.InvoicePaid =>
                    $"Invoice {row.InvoiceNo ?? ""} paid.",
                BillingActivityKinds.CreditNoteIssued =>
                    $"Credit note {row.CreditNoteNo ?? ""} issued.",
                BillingActivityKinds.PaymentMethodUpdated =>
                    $"{actor} updated the payment method.",
                BillingActivityKinds.SoftLockEntered =>
                    "Account entered Soft lock.",
                BillingActivityKinds.DormantEntered =>
                    "Account entered Dormant.",
                _ => "",
            };
        }

        private static string FormatCreditExpired(string? channel, int? qty)
        {
            var qtyChannel = FormatQtyChannel(channel, qty);
            if (qtyChannel == "")
            {
                return "";
            }

            var parts = qtyChannel.Split(' ', 2);
            return $"{parts[0]} purchased {parts[1]} expired.";
        }

        private static string FormatManualAdjust(
            string? channel,
            int? qty,
            string? direction
        )
        {
            var qtyChannel = FormatQtyChannel(channel, qty);
            if (direction == "remove")
            {
                return $"Tummly Support removed {qtyChannel}.";
            }

            return $"Tummly Support added {qtyChannel}.";
        }

        private static string FormatChannelCredits(string? channel, int? qty)
        {
            if (channel == null || qty == null)
            {
                return "";
            }

            var (singular, plural) = ChannelLabels(channel);
            return qty == 1
                ? $"1 {singular}"
                : $"{qty.Value.ToString("N0", CultureInfo.GetCultureInfo("en-GB"))} {plural}";
        }

        private static string FormatQtyChannel(string? channel, int? qty)
        {
            if (channel == null || qty == null)
            {
                return "";
            }

            var (_, plural) = ChannelLabels(channel);
            var qtyLabel = qty == 1
                ? "1"
                : qty.Value.ToString("N0", CultureInfo.GetCultureInfo("en-GB"));
            var channelLabel = qty == 1
                ? plural.Replace(" credits", " credit", StringComparison.Ordinal)
                : plural;
            return $"{qtyLabel} {channelLabel}";
        }

        private static (string Singular, string Plural) ChannelLabels(string channel)
        {
            return channel switch
            {
                CreditChannels.Email => ("Email credit", "Email credits"),
                CreditChannels.Sms => ("SMS credit", "SMS credits"),
                CreditChannels.Ai => ("AI credit", "AI credits"),
                _ => ($"{channel} credit", $"{channel} credits"),
            };
        }

        private static string FormatPlanAndOrCadence(string plan, string cadence)
        {
            if (plan != "" && cadence != "")
            {
                return $"{plan}, {cadence}";
            }

            if (plan != "")
            {
                return plan;
            }

            if (cadence != "")
            {
                return cadence;
            }

            return "";
        }
    }
}
