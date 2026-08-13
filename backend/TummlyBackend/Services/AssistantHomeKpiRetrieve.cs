using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AssistantHomeKpiRetrieve : IAssistantHomeKpiRetrieve
    {
        private readonly ApplicationDbContext _context;

        public AssistantHomeKpiRetrieve(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssistantHomeKpiRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var span = toUtc - fromUtc;
                var previousFromUtc = fromUtc - span;
                var previousToUtc = fromUtc;

                var feedbackSubmitted = await _context.Feedbacks
                    .CountAsync(
                        feedback =>
                            feedback.RestaurantLocationId == ownedLocationId
                            && feedback.CreatedAt >= fromUtc
                            && feedback.CreatedAt < toUtc,
                        cancellationToken
                    );

                var feedbackSubmittedPrevious = await _context.Feedbacks
                    .CountAsync(
                        feedback =>
                            feedback.RestaurantLocationId == ownedLocationId
                            && feedback.CreatedAt >= previousFromUtc
                            && feedback.CreatedAt < previousToUtc,
                        cancellationToken
                    );

                var guestsJoined = await _context.LocationGuests
                    .CountAsync(
                        guest =>
                            guest.RestaurantLocationId == ownedLocationId
                            && guest.CreatedAt >= fromUtc
                            && guest.CreatedAt < toUtc,
                        cancellationToken
                    );

                var guestsJoinedPrevious = await _context.LocationGuests
                    .CountAsync(
                        guest =>
                            guest.RestaurantLocationId == ownedLocationId
                            && guest.CreatedAt >= previousFromUtc
                            && guest.CreatedAt < previousToUtc,
                        cancellationToken
                    );

                var qrScans = await _context.QrScanEvents
                    .CountAsync(
                        scan =>
                            scan.RestaurantLocationId == ownedLocationId
                            && scan.CreatedAt >= fromUtc
                            && scan.CreatedAt < toUtc,
                        cancellationToken
                    );

                var qrScansPrevious = await _context.QrScanEvents
                    .CountAsync(
                        scan =>
                            scan.RestaurantLocationId == ownedLocationId
                            && scan.CreatedAt >= previousFromUtc
                            && scan.CreatedAt < previousToUtc,
                        cancellationToken
                    );

                return new AssistantHomeKpiRetrieveResult.Ok(
                    new AssistantHomeKpiEvidence(
                        feedbackSubmitted,
                        feedbackSubmittedPrevious,
                        guestsJoined,
                        guestsJoinedPrevious,
                        qrScans,
                        qrScansPrevious
                    )
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new AssistantHomeKpiRetrieveResult.Failed();
            }
        }
    }
}
