using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    internal static class CampaignBillingClose
    {
        public sealed record CloseResult(
            bool SettleFailed,
            bool ReleaseFailed,
            string? Message
        );

        public static async Task<CloseResult> CloseHoldAsync(
            Campaign entity,
            ICampaignBillingReserve billingReserve,
            ApplicationDbContext context,
            string channel,
            bool settleUnbilled,
            CancellationToken cancellationToken
        )
        {
            var reservationRef = entity.BillingReservationRef;
            if (string.IsNullOrWhiteSpace(reservationRef))
            {
                ClearHold(entity);
                return new CloseResult(false, false, null);
            }

            if (settleUnbilled)
            {
                var unbilled = await ComputeUnbilledAcceptedUnitsAsync(
                    context,
                    entity,
                    channel,
                    cancellationToken
                );
                if (unbilled > 0)
                {
                    var settle = await billingReserve.SettleAsync(
                        new CampaignBillingSettleRequest
                        {
                            CampaignId = entity.Id,
                            ReservationRef = reservationRef,
                            Channel = channel,
                            AcceptedUnits = unbilled,
                        },
                        cancellationToken
                    );
                    if (settle is CampaignBillingSettleResult.Failed failed)
                    {
                        return new CloseResult(true, false, failed.Message);
                    }

                    entity.SettledUnits += unbilled;
                }
            }

            var release = await billingReserve.ReleaseAsync(
                new CampaignBillingReleaseRequest
                {
                    CampaignId = entity.Id,
                    ReservationRef = reservationRef,
                },
                cancellationToken
            );
            if (release is CampaignBillingReleaseResult.Failed releaseFailed)
            {
                return new CloseResult(false, true, releaseFailed.Message);
            }

            ClearHold(entity);
            return new CloseResult(false, false, null);
        }

        public static async Task<(bool Succeeded, string? Message, int SettledUnits)> SettleUnbilledAsync(
            Campaign entity,
            ICampaignBillingReserve billingReserve,
            ApplicationDbContext context,
            string channel,
            CancellationToken cancellationToken
        )
        {
            var reservationRef = entity.BillingReservationRef;
            if (string.IsNullOrWhiteSpace(reservationRef))
            {
                return (true, null, 0);
            }

            var unbilled = await ComputeUnbilledAcceptedUnitsAsync(
                context,
                entity,
                channel,
                cancellationToken
            );
            if (unbilled <= 0)
            {
                return (true, null, 0);
            }

            var settle = await billingReserve.SettleAsync(
                new CampaignBillingSettleRequest
                {
                    CampaignId = entity.Id,
                    ReservationRef = reservationRef,
                    Channel = channel,
                    AcceptedUnits = unbilled,
                },
                cancellationToken
            );
            if (settle is CampaignBillingSettleResult.Failed failed)
            {
                return (false, failed.Message, 0);
            }

            entity.SettledUnits += unbilled;
            return (true, null, unbilled);
        }

        public static int AcceptedUnitsForDelivery(string channel, string? messageBody)
        {
            var normalized = (channel ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "sms")
            {
                return CampaignSmsSegmentCalculator.CountSegments(messageBody);
            }

            return 1;
        }

        private static async Task<int> ComputeUnbilledAcceptedUnitsAsync(
            ApplicationDbContext context,
            Campaign entity,
            string channel,
            CancellationToken cancellationToken
        )
        {
            var acceptedUnits = await context.CampaignRecipientDeliveries
                .Where(row =>
                    row.CampaignId == entity.Id
                    && row.Channel == channel
                    && row.Outcome == CampaignFireService.AcceptedOutcome
                )
                .Select(row => row.AcceptedUnits ?? 1)
                .ToListAsync(cancellationToken);

            var acceptedSum = acceptedUnits.Sum();
            return Math.Max(0, acceptedSum - entity.SettledUnits);
        }

        private static void ClearHold(Campaign entity)
        {
            entity.BillingReservationRef = null;
            entity.ReservedEstimate = null;
            entity.SettledUnits = 0;
        }
    }
}
