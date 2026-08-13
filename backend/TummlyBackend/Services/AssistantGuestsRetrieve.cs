using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AssistantGuestsRetrieve : IAssistantGuestsRetrieve
    {
        public const int MaxSampleRows = 100;

        private readonly ApplicationDbContext _context;

        public AssistantGuestsRetrieve(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssistantGuestsRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var scoped = _context.LocationGuests
                    .AsNoTracking()
                    .Where(guest => guest.RestaurantLocationId == ownedLocationId);

                var total = await scoped.CountAsync(cancellationToken);
                var page = await scoped
                    .OrderByDescending(guest => guest.CreatedAt)
                    .ThenByDescending(guest => guest.Id)
                    .Take(MaxSampleRows)
                    .Select(guest => new
                    {
                        guest.Id,
                        guest.Name,
                        guest.OffersOptOut,
                        Email = guest.MasterGuest != null ? guest.MasterGuest.Email : null,
                        Mobile = guest.MasterGuest != null ? guest.MasterGuest.Mobile : null,
                    })
                    .ToListAsync(cancellationToken);

                var ids = page.Select(guest => guest.Id).ToList();
                var tagsByGuest = await LoadTagsAsync(ids, cancellationToken);
                var redaction = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var rows = new List<AssistantGuestEvidenceRow>(page.Count);

                foreach (var guest in page)
                {
                    AddRedactionToken(redaction, guest.Email);
                    AddRedactionToken(redaction, guest.Mobile);
                    rows.Add(
                        new AssistantGuestEvidenceRow(
                            guest.Id,
                            guest.Name,
                            LocationGuestProjections.DeriveMarketingStatus(
                                guest.OffersOptOut,
                                guest.Email,
                                guest.Mobile
                            ),
                            tagsByGuest.GetValueOrDefault(guest.Id) ?? [],
                            LocationGuestProjections.IsMarketingEligible(
                                guest.OffersOptOut,
                                guest.Email,
                                guest.Mobile
                            )
                        )
                    );
                }

                return new AssistantGuestsRetrieveResult.Ok(
                    new AssistantGuestsEvidence(
                        total,
                        rows.Count,
                        rows,
                        redaction.ToList()
                    )
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new AssistantGuestsRetrieveResult.Failed();
            }
        }

        private async Task<Dictionary<int, IReadOnlyList<string>>> LoadTagsAsync(
            IReadOnlyList<int> guestIds,
            CancellationToken cancellationToken
        )
        {
            if (guestIds.Count == 0)
            {
                return [];
            }

            var ids = guestIds as List<int> ?? guestIds.ToList();
            var tagsByGuest = await _context.LocationGuestTags
                .AsNoTracking()
                .Where(membership => ids.Contains(membership.LocationGuestId))
                .Select(membership => new
                {
                    membership.LocationGuestId,
                    Name = membership.GuestTag != null
                        ? membership.GuestTag.DisplayName
                        : null,
                })
                .ToListAsync(cancellationToken);

            return tagsByGuest
                .Where(row => !string.IsNullOrWhiteSpace(row.Name))
                .GroupBy(row => row.LocationGuestId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group
                        .Select(row => row.Name!)
                        .Distinct(StringComparer.Ordinal)
                        .ToList()
                );
        }

        private static void AddRedactionToken(HashSet<string> tokens, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                tokens.Add(value.Trim());
            }
        }
    }
}
