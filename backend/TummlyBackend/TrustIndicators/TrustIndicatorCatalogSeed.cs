using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.TrustIndicators
{
    /// <summary>
    /// Seeds the 15 trust indicators in canonical order with the approved copy.
    /// All start disabled with no CTA. Inserts missing item numbers. Backfills a row only when it has
    /// never been edited (blank tooltip and status label), and never changes Enabled or CtaType.
    /// Creates no publication.
    /// </summary>
    public static class TrustIndicatorCatalogSeed
    {
        public sealed record SeedItem(
            int ItemNumber,
            string Title,
            TrustEvidenceClass EvidenceClass,
            TrustIndicatorStatus Status,
            string StatusLabel,
            string IconKey,
            string Tooltip,
            string? Issuer = null,
            string? EvidenceUrl = null,
            DateOnly? IssuedOn = null,
            string? Scope = null
        );

        public static readonly IReadOnlyList<SeedItem> Items =
        [
            new(
                1,
                "ICO Data Protection Registration",
                TrustEvidenceClass.VerifiedPublicRecord,
                TrustIndicatorStatus.VerifiedPublicRecord,
                "Registered",
                "ico-data",
                "Tummly.com Limited is registered with the Information Commissioner’s Office under reference ZC270005. This registration does not certify compliance with UK GDPR.",
                Issuer: "Information Commissioner’s Office",
                EvidenceUrl: "https://ico.org.uk/ESDWebPages/Entry/ZC270005"
            ),
            new(
                2,
                "ICO Data Protection Essentials",
                TrustEvidenceClass.VerifiedSelfAssessment,
                TrustIndicatorStatus.VerifiedCredential,
                "Self-assessment completed",
                "ico-dpe",
                "Tummly.com Limited completed the ICO Data Protection Essentials organisational self-assessment on 22 October 2026. This confirms programme completion, not independent verification of compliance with data protection law.",
                Issuer: "Information Commissioner’s Office",
                IssuedOn: new DateOnly(2026, 10, 22),
                Scope: "Organisational self-assessment, reference ICO-ASSESS-2026-B36B8DB2"
            ),
            new(
                3,
                "Cyber Essentials",
                TrustEvidenceClass.Roadmap,
                TrustIndicatorStatus.Roadmap,
                "On our roadmap",
                "roadmap-cyber-essentials",
                "Cyber Essentials certification is on Tummly’s cybersecurity roadmap. Tummly has not yet been assessed or certified under this scheme."
            ),
            new(
                4,
                "Cyber Essentials Plus",
                TrustEvidenceClass.Roadmap,
                TrustIndicatorStatus.Roadmap,
                "On our roadmap",
                "roadmap-cyber-essentials-plus",
                "Cyber Essentials Plus adds independent technical testing to the Cyber Essentials controls. Tummly has not achieved this certification; it is a future objective."
            ),
            new(
                5,
                "ISO/IEC 27001",
                TrustEvidenceClass.Roadmap,
                TrustIndicatorStatus.Roadmap,
                "On our roadmap",
                "roadmap-iso-27001",
                "ISO/IEC 27001 information security management certification is a future objective for Tummly. No independent certification has been achieved."
            ),
            new(
                6,
                "IASME Cyber Assurance Level One",
                TrustEvidenceClass.Roadmap,
                TrustIndicatorStatus.Roadmap,
                "On our roadmap",
                "roadmap-iasme",
                "IASME Cyber Assurance Level One is a verified cyber governance assessment. Tummly has not completed it. A current Cyber Essentials certificate is required first."
            ),
            new(
                7,
                "Good Business Charter",
                TrustEvidenceClass.Roadmap,
                TrustIndicatorStatus.Roadmap,
                "Application planned",
                "roadmap-gbc",
                "Tummly is assessing eligibility for the free Good Business Charter accreditation. The scheme requires evidence across ten responsible-business commitments. No award has been confirmed."
            ),
            new(
                8,
                "ISO/IEC 27701:2025",
                TrustEvidenceClass.Roadmap,
                TrustIndicatorStatus.Roadmap,
                "On our roadmap",
                "roadmap-iso-27701",
                "ISO/IEC 27701:2025 covers privacy information management. Independent certification is a future objective; Tummly has not been certified to this standard."
            ),
            new(
                9,
                "UK GDPR & Data Protection",
                TrustEvidenceClass.OwnControlQaPending,
                TrustIndicatorStatus.QaPending,
                "Under review",
                "gdpr-uk",
                "Our privacy notice describes how Tummly handles personal information, its relevant responsibilities and how privacy requests are handled. UK GDPR is a legal framework, not a general badge or certification."
            ),
            new(
                10,
                "Cookie & Privacy Choices",
                TrustEvidenceClass.OwnControlQaPending,
                TrustIndicatorStatus.QaPending,
                "Under review",
                "privacy-choices",
                "Visitors can read about site cookies and change their non-essential cookie choices using the website’s controls. This is not a third-party certification."
            ),
            new(
                11,
                "Permission-Based Marketing",
                TrustEvidenceClass.OwnControlQaPending,
                TrustIndicatorStatus.QaPending,
                "Under review",
                "opt-in-marketing",
                "Tummly distinguishes feedback follow-up permission from email and SMS marketing choices. Guest contact must satisfy recorded permissions and applicable suppression checks. This is a product safeguard, not a certification."
            ),
            new(
                12,
                "Guest Data Rights",
                TrustEvidenceClass.OwnControlQaPending,
                TrustIndicatorStatus.QaPending,
                "Under review",
                "data-rights",
                "Individuals can learn about access, correction and deletion requests where applicable. Responsibility for responding depends on the relevant controller and processing relationship."
            ),
            new(
                13,
                "UK Registered Company",
                TrustEvidenceClass.VerifiedPublicRecord,
                TrustIndicatorStatus.VerifiedPublicRecord,
                "Verified public record",
                "uk-company",
                "Tummly.com Limited is an active private limited company registered in England and Wales, company number 16236040. View the official Companies House record.",
                Issuer: "Companies House",
                EvidenceUrl: "https://find-and-update.company-information.service.gov.uk/company/16236040"
            ),
            new(
                14,
                "Encrypted HTTPS Connections",
                TrustEvidenceClass.OwnControlQaPending,
                TrustIndicatorStatus.QaPending,
                "Under review",
                "https-encrypted",
                "Tummly is verifying encrypted HTTPS connections across its public website and application. This is a technical security control, not independent certification."
            ),
            new(
                15,
                "Human-Controlled AI",
                TrustEvidenceClass.OwnControlQaPending,
                TrustIndicatorStatus.QaPending,
                "Under review",
                "ai-human",
                "Tummly is designing AI to support authorised operator decisions. Consequential actions require governed controls and operator confirmation. Final production behaviour is subject to verification; no independent certification is claimed."
            ),
        ];

        /// <summary>A new draft row with the approved seed copy, disabled and without a CTA.</summary>
        public static TrustIndicator CreateRow(SeedItem item, DateTime now)
        {
            var row = new TrustIndicator
            {
                ItemNumber = item.ItemNumber,
                Title = item.Title,
                EvidenceClass = item.EvidenceClass,
                Enabled = false,
                CtaType = TrustCtaType.None,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            ApplyCopy(row, item);
            return row;
        }

        public static async Task EnsureSeededAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken = default
        )
        {
            var existing = await context.TrustIndicators
                .ToDictionaryAsync(row => row.ItemNumber, cancellationToken);
            var now = DateTime.UtcNow;
            var changed = false;

            foreach (var item in Items)
            {
                if (!existing.TryGetValue(item.ItemNumber, out var row))
                {
                    context.TrustIndicators.Add(CreateRow(item, now));
                    changed = true;
                    continue;
                }

                // Blank copy means nobody has edited the row, so it takes the approved copy.
                if (row.Tooltip.Length == 0 && row.StatusLabel.Length == 0)
                {
                    ApplyCopy(row, item);
                    row.UpdatedAtUtc = now;
                    changed = true;
                }
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

        private static void ApplyCopy(TrustIndicator row, SeedItem item)
        {
            row.Status = item.Status;
            row.StatusLabel = item.StatusLabel;
            row.IconKey = item.IconKey;
            row.AltText = item.Title;
            row.Tooltip = item.Tooltip;
            row.Issuer = item.Issuer;
            row.EvidenceUrl = item.EvidenceUrl;
            row.IssuedOn = item.IssuedOn;
            row.Scope = item.Scope;
        }
    }
}
