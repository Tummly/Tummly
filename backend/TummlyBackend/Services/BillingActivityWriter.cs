using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class BillingActivityAppendRequest
    {
        public int RestaurantId { get; init; }

        public string Kind { get; init; } = string.Empty;

        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

        public string? ActorDisplayName { get; init; }

        public string? Channel { get; init; }

        public int? Qty { get; init; }

        public string? CampaignName { get; init; }

        public string? InvoiceNo { get; init; }

        public string? CreditNoteNo { get; init; }

        public string? Plan { get; init; }

        public string? Cadence { get; init; }

        public string? ScheduledDateLabel { get; init; }

        public string? LocationName { get; init; }

        public string? ManualAdjustDirection { get; init; }

        public string? ConsumeSource { get; init; }

        public string? FromPlan { get; init; }

        public string? FromCadence { get; init; }

        public string? ToPlan { get; init; }

        public string? ToCadence { get; init; }
    }

    public static class BillingActivityWriter
    {
        private static readonly HashSet<string> KnownKinds =
        [
            BillingActivityKinds.SubscriptionCreated,
            BillingActivityKinds.SubscriptionUpgraded,
            BillingActivityKinds.SubscriptionChangeScheduled,
            BillingActivityKinds.AdditionalLocationAdded,
            BillingActivityKinds.AdditionalLocationRemoveScheduled,
            BillingActivityKinds.SubscriptionCancelled,
            BillingActivityKinds.SubscriptionRenewed,
            BillingActivityKinds.TopupPurchased,
            BillingActivityKinds.TopupRefunded,
            BillingActivityKinds.CreditConsumed,
            BillingActivityKinds.CreditExpired,
            BillingActivityKinds.ManualCreditAdjusted,
            BillingActivityKinds.InvoicePaid,
            BillingActivityKinds.CreditNoteIssued,
            BillingActivityKinds.PaymentMethodUpdated,
            BillingActivityKinds.SoftLockEntered,
            BillingActivityKinds.DormantEntered,
        ];

        public static bool TryAppend(
            ApplicationDbContext context,
            BillingActivityAppendRequest request
        )
        {
            if (!KnownKinds.Contains(request.Kind))
            {
                return false;
            }

            if (FromEqualsTo(request))
            {
                return false;
            }

            context.RestaurantBillingActivities.Add(
                new RestaurantBillingActivity
                {
                    RestaurantId = request.RestaurantId,
                    Kind = request.Kind,
                    OccurredAtUtc = request.OccurredAtUtc,
                    ActorDisplayName = request.ActorDisplayName,
                    Channel = request.Channel,
                    Qty = request.Qty,
                    CampaignName = request.CampaignName,
                    InvoiceNo = request.InvoiceNo,
                    CreditNoteNo = request.CreditNoteNo,
                    Plan = request.Plan,
                    Cadence = request.Cadence,
                    ScheduledDateLabel = request.ScheduledDateLabel,
                    LocationName = request.LocationName,
                    ManualAdjustDirection = request.ManualAdjustDirection,
                    ConsumeSource = request.ConsumeSource,
                    FromPlan = request.FromPlan,
                    FromCadence = request.FromCadence,
                    ToPlan = request.ToPlan,
                    ToCadence = request.ToCadence,
                }
            );
            return true;
        }

        private static bool FromEqualsTo(BillingActivityAppendRequest request)
        {
            if (
                request.FromPlan != null
                || request.ToPlan != null
                || request.FromCadence != null
                || request.ToCadence != null
            )
            {
                return string.Equals(
                    request.FromPlan,
                    request.ToPlan,
                    StringComparison.Ordinal
                )
                    && string.Equals(
                        request.FromCadence,
                        request.ToCadence,
                        StringComparison.Ordinal
                    );
            }

            return false;
        }
    }
}
