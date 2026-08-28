using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Live Campaign Billing Reserve / Settle / Release adapter (ticket 19).
    /// Maps Campaign LocationId to RestaurantId and writes CreditLedgerEntries.
    /// </summary>
    public sealed class LiveCampaignBillingReserve : ICampaignBillingReserve
    {
        private readonly ApplicationDbContext _context;
        private readonly ICreditLedger _ledger;

        public LiveCampaignBillingReserve(
            ApplicationDbContext context,
            ICreditLedger ledger
        )
        {
            _context = context;
            _ledger = ledger;
        }

        public bool IsLive => true;

        public async Task<CampaignBillingReserveResult> ReserveAsync(
            CampaignBillingReserveRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var resolved = await ResolveRestaurantAsync(
                request.LocationId,
                cancellationToken
            );
            if (resolved == null)
            {
                return Failed("location_not_in_account");
            }

            var result = await _ledger.ReserveAsync(
                new CreditLedgerReserveRequest
                {
                    RestaurantId = resolved.Value,
                    Channel = NormalizeChannel(request.Channel),
                    Units = request.Units,
                    LocationId = request.LocationId,
                },
                cancellationToken
            );

            if (!result.Succeeded)
            {
                return Failed(result.Code ?? "insufficient_credits");
            }

            if (string.IsNullOrWhiteSpace(result.ReservationRef))
            {
                return Failed("insufficient_credits");
            }

            return new CampaignBillingReserveResult.Ok
            {
                ReservationRef = result.ReservationRef,
            };
        }

        public async Task<CampaignBillingSettleResult> SettleAsync(
            CampaignBillingSettleRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var locationId = await ResolveLocationForCampaignAsync(
                request.CampaignId,
                cancellationToken
            );
            if (locationId == null)
            {
                return new CampaignBillingSettleResult.Failed
                {
                    Message = "Campaign not found.",
                };
            }

            var resolved = await ResolveRestaurantAsync(
                locationId.Value,
                cancellationToken
            );
            if (resolved == null)
            {
                return new CampaignBillingSettleResult.Failed
                {
                    Message = "location_not_in_account",
                };
            }

            var result = await _ledger.SettleAsync(
                new CreditLedgerSettleRequest
                {
                    RestaurantId = resolved.Value,
                    ReservationRef = request.ReservationRef,
                    Channel = NormalizeChannel(request.Channel),
                    AcceptedUnits = request.AcceptedUnits,
                    LocationId = locationId.Value,
                },
                cancellationToken
            );

            if (!result.Succeeded)
            {
                return new CampaignBillingSettleResult.Failed
                {
                    Message = result.Code ?? "reservation_closed",
                };
            }

            return new CampaignBillingSettleResult.Ok();
        }

        public async Task<CampaignBillingReleaseResult> ReleaseAsync(
            CampaignBillingReleaseRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var locationId = await ResolveLocationForCampaignAsync(
                request.CampaignId,
                cancellationToken
            );
            if (locationId == null)
            {
                return new CampaignBillingReleaseResult.Failed
                {
                    Message = "Campaign not found.",
                };
            }

            var resolved = await ResolveRestaurantAsync(
                locationId.Value,
                cancellationToken
            );
            if (resolved == null)
            {
                return new CampaignBillingReleaseResult.Failed
                {
                    Message = "location_not_in_account",
                };
            }

            var channel = await ResolveCampaignChannelAsync(
                request.CampaignId,
                cancellationToken
            );
            if (channel == null)
            {
                return new CampaignBillingReleaseResult.Failed
                {
                    Message = "Campaign not found.",
                };
            }

            var result = await _ledger.ReleaseAsync(
                new CreditLedgerReleaseRequest
                {
                    RestaurantId = resolved.Value,
                    ReservationRef = request.ReservationRef,
                    Channel = channel,
                    LocationId = locationId.Value,
                },
                cancellationToken
            );

            if (!result.Succeeded)
            {
                return new CampaignBillingReleaseResult.Failed
                {
                    Message = result.Code ?? "release_failed",
                };
            }

            return new CampaignBillingReleaseResult.Ok();
        }

        private async Task<int?> ResolveRestaurantAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            return await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<int?> ResolveLocationForCampaignAsync(
            int campaignId,
            CancellationToken cancellationToken
        )
        {
            return await _context.Campaigns
                .AsNoTracking()
                .Where(row => row.Id == campaignId)
                .Select(row => (int?)row.RestaurantLocationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<string?> ResolveCampaignChannelAsync(
            int campaignId,
            CancellationToken cancellationToken
        )
        {
            var channel = await _context.Campaigns
                .AsNoTracking()
                .Where(row => row.Id == campaignId)
                .Select(row => row.Channel)
                .FirstOrDefaultAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(channel)
                ? null
                : NormalizeChannel(channel);
        }

        private static string NormalizeChannel(string channel)
        {
            return channel.Trim().ToLowerInvariant();
        }

        private static CampaignBillingReserveResult Failed(string code)
        {
            return new CampaignBillingReserveResult.Failed
            {
                Message = code,
            };
        }
    }
}
