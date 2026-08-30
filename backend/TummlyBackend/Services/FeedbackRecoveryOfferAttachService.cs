using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Persist Recovery OfferId on Feedback before Send (ticket 02).
    /// </summary>
    public class FeedbackRecoveryOfferAttachService
        : IFeedbackRecoveryOfferAttachService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOffersCatalogService _offers;

        public FeedbackRecoveryOfferAttachService(
            ApplicationDbContext context,
            IOffersCatalogService offers
        )
        {
            _context = context;
            _offers = offers;
        }

        public async Task<int?> GetAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            return await _context.Feedbacks
                .AsNoTracking()
                .Where(feedback => feedback.Id == feedbackId)
                .Select(feedback => feedback.RecoveryOfferId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task SetAsync(
            int feedbackId,
            int? offerId,
            CancellationToken cancellationToken = default
        )
        {
            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(
                    row => row.Id == feedbackId,
                    cancellationToken
                );

            if (feedback == null)
            {
                throw new InvalidOperationException("Feedback not found.");
            }

            if (offerId is null)
            {
                var previousOfferId = feedback.RecoveryOfferId;
                feedback.RecoveryOfferId = null;
                await _context.SaveChangesAsync(cancellationToken);
                await _offers.SyncInFlightStoredStatusForAttachChangeAsync(
                    previousOfferId,
                    nextOfferId: null,
                    cancellationToken
                );
                return;
            }

            if (offerId.Value < 1)
            {
                throw new ArgumentException("offerId is invalid.");
            }

            var ok = await _offers.IsAttachableForLocationAsync(
                offerId.Value,
                feedback.RestaurantLocationId,
                cancellationToken
            );
            if (!ok)
            {
                throw new ArgumentException(
                    "offerId must reference an attachable Draft or Active Offers catalog definition for this location."
                );
            }

            var replacedOfferId = feedback.RecoveryOfferId;
            feedback.RecoveryOfferId = offerId.Value;
            await _context.SaveChangesAsync(cancellationToken);
            var sync = await _offers.SyncInFlightStoredStatusForAttachChangeAsync(
                replacedOfferId,
                offerId.Value,
                cancellationToken
            );
            if (sync is not CatalogOfferInFlightSyncResult.Ok)
            {
                feedback.RecoveryOfferId = replacedOfferId;
                await _context.SaveChangesAsync(cancellationToken);
                throw PlanEntitlementCapException.FromSync(sync);
            }
        }
    }
}
