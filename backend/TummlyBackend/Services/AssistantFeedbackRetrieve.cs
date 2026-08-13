using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AssistantFeedbackRetrieve : IAssistantFeedbackRetrieve
    {
        public const int MaxSampleRows = 100;

        private readonly ApplicationDbContext _context;

        public AssistantFeedbackRetrieve(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssistantFeedbackRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var window = _context.Feedbacks
                    .AsNoTracking()
                    .Where(feedback =>
                        feedback.RestaurantLocationId == ownedLocationId
                        && feedback.CreatedAt >= fromUtc
                        && feedback.CreatedAt < toUtc
                    );

                var snapshot = await window
                    .Select(feedback => new
                    {
                        feedback.ClassificationStatus,
                        feedback.Sentiment,
                        feedback.WorkflowStatus,
                        feedback.DetectedTagsJson,
                    })
                    .ToListAsync(cancellationToken);

                var rows = await window
                    .OrderByDescending(feedback => feedback.CreatedAt)
                    .ThenByDescending(feedback => feedback.Id)
                    .Take(MaxSampleRows)
                    .Select(feedback => new
                    {
                        feedback.Id,
                        feedback.CreatedAt,
                        feedback.GuestName,
                        feedback.GuestContact,
                        feedback.Sentiment,
                        feedback.ClassificationStatus,
                        feedback.DetectedTagsJson,
                        feedback.WorkflowStatus,
                        feedback.Comment,
                        feedback.ContactType,
                        feedback.LocationGuestId,
                        feedback.QrCodeId,
                    })
                    .ToListAsync(cancellationToken);

                var qrById = await LoadQrSourcesAsync(
                    rows.Select(row => row.QrCodeId).Distinct().ToList(),
                    cancellationToken
                );

                var placeholder4Ids = await window
                    .Where(feedback =>
                        feedback.ClassificationStatus == ClassificationStatus.Succeeded
                        && feedback.Sentiment == FeedbackSentiment.Negative
                        && feedback.LocationGuestId != null
                    )
                    .Select(feedback => feedback.LocationGuestId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var sampleGuestIds = rows
                    .Where(row => row.LocationGuestId is int)
                    .Select(row => row.LocationGuestId!.Value)
                    .Distinct()
                    .ToList();

                var guestIds = sampleGuestIds
                    .Concat(placeholder4Ids)
                    .Distinct()
                    .ToList();

                var guestFacts = await LoadGuestFactsAsync(guestIds, cancellationToken);

                var tagCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                var succeededPositive = 0;
                var succeededNeutral = 0;
                var succeededNegative = 0;
                var needsAttention = 0;

                foreach (var row in snapshot)
                {
                    if (row.ClassificationStatus == ClassificationStatus.Succeeded
                        && row.Sentiment == FeedbackSentiment.Negative
                        && row.WorkflowStatus != FeedbackWorkflowStatus.Resolved)
                    {
                        needsAttention++;
                    }

                    if (row.ClassificationStatus == ClassificationStatus.Succeeded)
                    {
                        switch (row.Sentiment)
                        {
                            case FeedbackSentiment.Positive:
                                succeededPositive++;
                                break;
                            case FeedbackSentiment.Neutral:
                                succeededNeutral++;
                                break;
                            case FeedbackSentiment.Negative:
                                succeededNegative++;
                                break;
                        }

                        foreach (var tag in ParseTags(row.DetectedTagsJson))
                        {
                            tagCounts[tag] = tagCounts.GetValueOrDefault(tag) + 1;
                        }
                    }
                }

                var redaction = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var evidenceRows = rows
                    .Select(row =>
                    {
                        AddRedactionToken(redaction, row.GuestContact);

                        var succeeded =
                            row.ClassificationStatus == ClassificationStatus.Succeeded;
                        var tags = succeeded ? ParseTags(row.DetectedTagsJson) : [];
                        var needs =
                            succeeded
                            && row.Sentiment == FeedbackSentiment.Negative
                            && row.WorkflowStatus != FeedbackWorkflowStatus.Resolved;
                        var linked = row.LocationGuestId is int guestId
                            && guestFacts.ContainsKey(guestId);
                        GuestFact? fact = linked
                            ? guestFacts[row.LocationGuestId!.Value]
                            : null;
                        if (fact is not null)
                        {
                            AddRedactionToken(redaction, fact.Email);
                            AddRedactionToken(redaction, fact.Mobile);
                        }

                        var qrSource = qrById.GetValueOrDefault(row.QrCodeId);

                        return new AssistantFeedbackEvidenceRow(
                            row.Id,
                            row.CreatedAt,
                            row.GuestName,
                            succeeded ? row.Sentiment?.ToString().ToLowerInvariant() : null,
                            row.ClassificationStatus.ToString(),
                            tags,
                            row.WorkflowStatus.ToString(),
                            needs,
                            qrSource,
                            ContactTypeLabel(row.ContactType),
                            Excerpt(row.Comment),
                            FeedbackReference(row.Id),
                            fact?.MarketingStatus,
                            fact?.GuestTags ?? [],
                            linked ? row.LocationGuestId : null,
                            linked
                        );
                    })
                    .ToList();

                var guestRows = DistinctGuests(sampleGuestIds, guestFacts);
                var placeholder4Rows = DistinctGuests(placeholder4Ids, guestFacts)
                    .Where(guest => guest.IsMarketingEligible)
                    .ToList();

                return new AssistantFeedbackRetrieveResult.Ok(
                    new AssistantFeedbackEvidence(
                        snapshot.Count,
                        evidenceRows.Count,
                        succeededPositive,
                        succeededNeutral,
                        succeededNegative,
                        needsAttention,
                        tagCounts
                            .OrderByDescending(pair => pair.Value)
                            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                            .Select(pair => new AssistantFeedbackTagCount(pair.Key, pair.Value))
                            .ToList(),
                        evidenceRows,
                        guestRows,
                        placeholder4Rows,
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
                return new AssistantFeedbackRetrieveResult.Failed();
            }
        }

        private async Task<Dictionary<int, string?>> LoadQrSourcesAsync(
            IReadOnlyList<int> qrCodeIds,
            CancellationToken cancellationToken
        )
        {
            var ids = qrCodeIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
            {
                return [];
            }

            var codes = await _context.QrCodes
                .AsNoTracking()
                .Where(code => ids.Contains(code.Id))
                .ToListAsync(cancellationToken);

            return codes.ToDictionary(
                code => code.Id,
                code => FeedbackQrSourceMapping.ToDisplay(code)
            );
        }

        private async Task<Dictionary<int, GuestFact>> LoadGuestFactsAsync(
            IReadOnlyList<int> guestIds,
            CancellationToken cancellationToken
        )
        {
            if (guestIds.Count == 0)
            {
                return [];
            }

            var ids = guestIds as List<int> ?? guestIds.ToList();
            var guests = await _context.LocationGuests
                .AsNoTracking()
                .Where(guest => ids.Contains(guest.Id))
                .Select(guest => new
                {
                    guest.Id,
                    guest.Name,
                    guest.OffersOptOut,
                    Email = guest.MasterGuest != null ? guest.MasterGuest.Email : null,
                    Mobile = guest.MasterGuest != null ? guest.MasterGuest.Mobile : null,
                })
                .ToListAsync(cancellationToken);

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

            var tagMap = tagsByGuest
                .Where(row => !string.IsNullOrWhiteSpace(row.Name))
                .GroupBy(row => row.LocationGuestId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group
                        .Select(row => row.Name!)
                        .Distinct(StringComparer.Ordinal)
                        .ToList()
                );

            var facts = new Dictionary<int, GuestFact>();
            foreach (var guest in guests)
            {
                var eligible = LocationGuestProjections.IsMarketingEligible(
                    guest.OffersOptOut,
                    guest.Email,
                    guest.Mobile
                );
                facts[guest.Id] = new GuestFact(
                    guest.Name,
                    LocationGuestProjections.DeriveMarketingStatus(
                        guest.OffersOptOut,
                        guest.Email,
                        guest.Mobile
                    ),
                    tagMap.GetValueOrDefault(guest.Id) ?? [],
                    eligible,
                    guest.Email,
                    guest.Mobile
                );
            }

            return facts;
        }

        private static IReadOnlyList<AssistantGuestEvidenceRow> DistinctGuests(
            IReadOnlyList<int> guestIds,
            IReadOnlyDictionary<int, GuestFact> facts
        )
        {
            var rows = new List<AssistantGuestEvidenceRow>();
            var seen = new HashSet<int>();
            foreach (var id in guestIds)
            {
                if (!seen.Add(id) || !facts.TryGetValue(id, out var fact))
                {
                    continue;
                }

                rows.Add(
                    new AssistantGuestEvidenceRow(
                        id,
                        fact.Name,
                        fact.MarketingStatus,
                        fact.GuestTags,
                        fact.IsMarketingEligible
                    )
                );
            }

            return rows;
        }

        private static void AddRedactionToken(HashSet<string> tokens, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                tokens.Add(value.Trim());
            }
        }

        private static string ContactTypeLabel(ContactType contactType)
            => contactType switch
            {
                ContactType.Email => "Email",
                ContactType.Phone => "Phone",
                _ => "Unknown",
            };

        private static IReadOnlyList<string> ParseTags(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static string Excerpt(string comment)
        {
            var trimmed = comment.Trim();
            if (trimmed.Length <= 80)
            {
                return trimmed;
            }

            return trimmed[..80] + "…";
        }

        private static string FeedbackReference(int id)
            => $"FDB-{id.ToString().PadLeft(6, '0')}";

        private sealed record GuestFact(
            string Name,
            string MarketingStatus,
            IReadOnlyList<string> GuestTags,
            bool IsMarketingEligible,
            string? Email,
            string? Mobile
        );
    }
}
