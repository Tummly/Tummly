using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.TrustIndicators
{
    /// <summary>
    /// Seeds the 15 trust indicators from spec §8, in canonical order.
    /// All start disabled with no evidence URLs. Inserts only missing item numbers and never
    /// overwrites an edited row. Creates no publication.
    /// </summary>
    public static class TrustIndicatorCatalogSeed
    {
        public static readonly IReadOnlyList<(
            int ItemNumber,
            string Title,
            TrustEvidenceClass EvidenceClass
        )> Items =
        [
            (1, "ICO Data Protection Registration", TrustEvidenceClass.VerifiedPublicRecord),
            (2, "ICO Data Protection Essentials", TrustEvidenceClass.VerifiedSelfAssessment),
            (3, "Cyber Essentials", TrustEvidenceClass.Roadmap),
            (4, "Cyber Essentials Plus", TrustEvidenceClass.Roadmap),
            (5, "ISO/IEC 27001", TrustEvidenceClass.Roadmap),
            (6, "IASME Cyber Assurance Level One", TrustEvidenceClass.Roadmap),
            (7, "Good Business Charter", TrustEvidenceClass.Roadmap),
            (8, "ISO/IEC 27701:2025", TrustEvidenceClass.Roadmap),
            (9, "UK GDPR & Data Protection", TrustEvidenceClass.OwnControlQaPending),
            (10, "Cookie & Privacy Choices", TrustEvidenceClass.OwnControlQaPending),
            (11, "Permission-Based Marketing", TrustEvidenceClass.OwnControlQaPending),
            (12, "Guest Data Rights", TrustEvidenceClass.OwnControlQaPending),
            (13, "UK Registered Company", TrustEvidenceClass.VerifiedPublicRecord),
            (14, "Encrypted HTTPS Connections", TrustEvidenceClass.OwnControlQaPending),
            (15, "Human-Controlled AI", TrustEvidenceClass.OwnControlQaPending),
        ];

        public static async Task EnsureSeededAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken = default
        )
        {
            var existing = await context.TrustIndicators
                .Select(row => row.ItemNumber)
                .ToListAsync(cancellationToken);
            var existingSet = existing.ToHashSet();
            var now = DateTime.UtcNow;
            var changed = false;

            foreach (var item in Items)
            {
                if (existingSet.Contains(item.ItemNumber))
                {
                    continue;
                }

                context.TrustIndicators.Add(
                    new TrustIndicator
                    {
                        ItemNumber = item.ItemNumber,
                        Title = item.Title,
                        EvidenceClass = item.EvidenceClass,
                        Enabled = false,
                        CtaType = TrustCtaType.None,
                        AltText = string.Empty,
                        Tooltip = string.Empty,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                    }
                );
                changed = true;
            }

            var stateExists = await context.TrustIndicatorDraftStates.AnyAsync(
                row => row.Id == TrustIndicatorRules.DraftStateId,
                cancellationToken
            );
            if (!stateExists)
            {
                context.TrustIndicatorDraftStates.Add(
                    new TrustIndicatorDraftState { Id = TrustIndicatorRules.DraftStateId }
                );
                changed = true;
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
