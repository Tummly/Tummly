using System.Text.RegularExpressions;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.TrustIndicators
{
    /// <summary>
    /// Server-side rules for trust indicator drafts and publication (ADM-15-02, 15-05, 15-08).
    /// </summary>
    public static class TrustIndicatorRules
    {
        public const int ItemCount = 15;

        public const int DraftStateId = 1;

        public const int MaxReasonLength = 500;

        private static readonly Regex CertifiedWording = new(
            @"\bcertif(ied|icate|ication)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );

        private static readonly Regex VerifiedWording = new(
            @"\b(verified|certified)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );

        public static DateOnly LondonToday(DateTime utcNow) =>
            DateOnly.FromDateTime(LondonDateFormat.ToLondonLocal(utcNow));

        /// <summary>Enabled and not past its expiry date (the expiry day itself is still live).</summary>
        public static bool IsLive(bool enabled, DateOnly? expiresOn, DateOnly today) =>
            enabled && (expiresOn is not { } expiry || expiry >= today);

        /// <summary>CONTRACT §1: the statuses an evidence class may hold.</summary>
        public static IReadOnlyList<TrustIndicatorStatus> AllowedStatuses(
            TrustEvidenceClass evidenceClass
        ) =>
            evidenceClass switch
            {
                TrustEvidenceClass.VerifiedPublicRecord =>
                [
                    TrustIndicatorStatus.VerifiedPublicRecord,
                    TrustIndicatorStatus.UnderReview,
                ],
                TrustEvidenceClass.VerifiedSelfAssessment =>
                [
                    TrustIndicatorStatus.VerifiedCredential,
                    TrustIndicatorStatus.UnderReview,
                ],
                TrustEvidenceClass.Roadmap => [TrustIndicatorStatus.Roadmap],
                TrustEvidenceClass.OwnControlQaPending =>
                [
                    TrustIndicatorStatus.QaPending,
                    TrustIndicatorStatus.QaPassed,
                ],
                _ => [],
            };

        /// <summary>
        /// CONTRACT §2: the public status for a stored status, or null when the item must be omitted
        /// (missing, unknown, or not allowed for the evidence class).
        /// </summary>
        public static string? PublicStatus(TrustEvidenceClass evidenceClass, string? status)
        {
            if (
                !TryParseExact(status, out TrustIndicatorStatus value)
                || !AllowedStatuses(evidenceClass).Contains(value)
            )
            {
                return null;
            }

            return value switch
            {
                TrustIndicatorStatus.VerifiedPublicRecord
                    or TrustIndicatorStatus.VerifiedCredential
                    or TrustIndicatorStatus.QaPassed => "Verified",
                TrustIndicatorStatus.Roadmap => "Roadmap",
                TrustIndicatorStatus.QaPending or TrustIndicatorStatus.UnderReview => "UnderReview",
                _ => null,
            };
        }

        /// <summary>
        /// CONTRACT §3: true only when the item is public-Verified and its CTA type, label and target fit
        /// the evidence class. False for <see cref="TrustCtaType.None"/>, because no CTA is shown.
        /// </summary>
        public static bool PublicCtaAllowed(
            TrustEvidenceClass evidenceClass,
            TrustIndicatorEditableValues values
        )
        {
            if (PublicStatus(evidenceClass, values.Status) != "Verified")
            {
                return false;
            }

            if (!TryParseExact(values.CtaType?.Trim(), out TrustCtaType ctaType))
            {
                return false;
            }

            var label = values.CtaLabel?.Trim();
            if (string.IsNullOrEmpty(label) || label.Length > 60)
            {
                return false;
            }

            return ctaType switch
            {
                TrustCtaType.ExternalProof =>
                    evidenceClass is TrustEvidenceClass.VerifiedPublicRecord
                        or TrustEvidenceClass.VerifiedSelfAssessment
                    && IsHttpsUrl(values.CtaTarget)
                    && string.Equals(values.CtaTarget, values.EvidenceUrl, StringComparison.Ordinal),
                TrustCtaType.InternalPage =>
                    evidenceClass == TrustEvidenceClass.OwnControlQaPending
                    && IsRootPath(values.CtaTarget),
                TrustCtaType.CookieSettings =>
                    evidenceClass == TrustEvidenceClass.OwnControlQaPending
                    && values.CtaTarget == null,
                _ => false,
            };
        }

        public static TrustIndicatorEditableValues ToValues(TrustIndicator row) =>
            new()
            {
                Enabled = row.Enabled,
                Status = row.Status.ToString(),
                StatusLabel = row.StatusLabel,
                CtaType = row.CtaType.ToString(),
                CtaLabel = row.CtaLabel,
                CtaTarget = row.CtaTarget,
                EvidenceUrl = row.EvidenceUrl,
                ImageUrl = row.ImageUrl,
                ImageRightsNote = row.ImageRightsNote,
                IconKey = row.IconKey,
                AltText = row.AltText,
                Tooltip = row.Tooltip,
                Issuer = row.Issuer,
                Scope = row.Scope,
                IssuedOn = row.IssuedOn,
                ExpiresOn = row.ExpiresOn,
                ReviewOn = row.ReviewOn,
            };

        public static void Apply(TrustIndicator row, TrustIndicatorEditableValues values)
        {
            row.Enabled = values.Enabled;
            row.Status = Enum.Parse<TrustIndicatorStatus>(values.Status!);
            row.StatusLabel = values.StatusLabel ?? string.Empty;
            row.CtaType = Enum.Parse<TrustCtaType>(values.CtaType!);
            row.CtaLabel = values.CtaLabel;
            row.CtaTarget = values.CtaTarget;
            row.EvidenceUrl = values.EvidenceUrl;
            row.ImageUrl = values.ImageUrl;
            row.ImageRightsNote = values.ImageRightsNote;
            row.IconKey = values.IconKey;
            row.AltText = values.AltText ?? string.Empty;
            row.Tooltip = values.Tooltip ?? string.Empty;
            row.Issuer = values.Issuer;
            row.Scope = values.Scope;
            row.IssuedOn = values.IssuedOn;
            row.ExpiresOn = values.ExpiresOn;
            row.ReviewOn = values.ReviewOn;
        }

        /// <summary>
        /// Trims input, blanks optional text to null and checks formats and lengths.
        /// Adds one message per problem to <paramref name="errors"/>.
        /// </summary>
        public static TrustIndicatorEditableValues Normalize(
            TrustEvidenceClass evidenceClass,
            TrustIndicatorEditableValues input,
            List<string> errors
        )
        {
            var allowedStatuses = AllowedStatuses(evidenceClass);
            var statusKnown = TryParseExact(input.Status?.Trim(), out TrustIndicatorStatus status);
            if (!statusKnown)
            {
                errors.Add(
                    "Status must be one of VerifiedPublicRecord, VerifiedCredential, Roadmap, QaPending, QaPassed or UnderReview."
                );
            }
            else if (!allowedStatuses.Contains(status))
            {
                errors.Add($"Status {status} is not allowed for {evidenceClass} items.");
            }

            // Exact names only: Enum.TryParse would also accept "ExternalProof, None".
            var ctaTypeRaw = input.CtaType?.Trim() ?? string.Empty;
            var ctaTypeKnown = TryParseExact(ctaTypeRaw, out TrustCtaType ctaType);
            if (!ctaTypeKnown)
            {
                errors.Add("CTA type must be None, ExternalProof, InternalPage or CookieSettings.");
                ctaType = TrustCtaType.None;
            }

            var statusLabel = Trim(input.StatusLabel) ?? string.Empty;
            var ctaLabel = Trim(input.CtaLabel);
            var ctaTarget = Trim(input.CtaTarget);
            var evidenceUrl = Trim(input.EvidenceUrl);
            var imageUrl = Trim(input.ImageUrl);
            var imageRightsNote = Trim(input.ImageRightsNote);
            var iconKey = Trim(input.IconKey);
            var altText = Trim(input.AltText);
            var tooltip = Trim(input.Tooltip);
            var issuer = Trim(input.Issuer);
            var scope = Trim(input.Scope);

            if (ctaType is TrustCtaType.None or TrustCtaType.CookieSettings)
            {
                ctaTarget = null;
            }
            else if (ctaType == TrustCtaType.ExternalProof && !IsHttpsUrl(ctaTarget))
            {
                errors.Add("External proof CTA needs an https link.");
            }
            else if (ctaType == TrustCtaType.InternalPage && !IsRootPath(ctaTarget))
            {
                errors.Add("Internal page CTA needs a path that starts with /.");
            }

            if (evidenceUrl != null && !IsHttpsUrl(evidenceUrl))
            {
                errors.Add("Evidence URL must be an https link.");
            }

            if (imageUrl != null && evidenceClass == TrustEvidenceClass.Roadmap)
            {
                errors.Add("Roadmap items cannot have an image.");
            }

            if (imageUrl != null && !IsHttpsUrl(imageUrl) && !IsRootPath(imageUrl))
            {
                errors.Add("Image must be an https link or a path that starts with /.");
            }

            if (imageUrl != null && altText == null)
            {
                errors.Add("Alt text is required when an image is set.");
            }

            if (imageUrl != null && imageRightsNote == null)
            {
                errors.Add("Image rights note is required when an image is set.");
            }

            AddLengthError(errors, "Status label", statusLabel, 60);
            AddLengthError(errors, "CTA label", ctaLabel, 60);
            AddLengthError(errors, "Image rights note", imageRightsNote, 300);
            AddLengthError(errors, "Icon key", iconKey, 64);
            AddLengthError(errors, "CTA target", ctaTarget, 500);
            AddLengthError(errors, "Evidence URL", evidenceUrl, 500);
            AddLengthError(errors, "Image URL", imageUrl, 500);
            AddLengthError(errors, "Alt text", altText, 300);
            AddLengthError(errors, "Tooltip", tooltip, 300);
            AddLengthError(errors, "Issuer", issuer, 200);
            AddLengthError(errors, "Scope", scope, 300);

            if (input.IssuedOn is { } issued && input.ExpiresOn is { } expires && expires < issued)
            {
                errors.Add("Expiry date cannot be before the issued date.");
            }

            return new TrustIndicatorEditableValues
            {
                Enabled = input.Enabled,
                Status = (statusKnown ? status : TrustIndicatorStatus.QaPending).ToString(),
                StatusLabel = statusLabel,
                CtaType = ctaType.ToString(),
                CtaLabel = ctaLabel,
                CtaTarget = ctaTarget,
                EvidenceUrl = evidenceUrl,
                ImageUrl = imageUrl,
                ImageRightsNote = imageRightsNote,
                IconKey = iconKey,
                AltText = altText ?? string.Empty,
                Tooltip = tooltip ?? string.Empty,
                Issuer = issuer,
                Scope = scope,
                IssuedOn = input.IssuedOn,
                ExpiresOn = input.ExpiresOn,
                ReviewOn = input.ReviewOn,
            };
        }

        /// <summary>
        /// Publish gate (CONTRACT §5, items 1 to 5). Only enabled items are checked. The evidence link
        /// check (item 6) runs in the service. Empty list means the draft may publish.
        /// </summary>
        public static IReadOnlyList<string> PublishGateErrors(
            IReadOnlyList<TrustIndicator> items,
            DateOnly today
        )
        {
            var errors = new List<string>();
            foreach (var item in items.OrderBy(row => row.ItemNumber))
            {
                if (!item.Enabled)
                {
                    continue;
                }

                var label = $"Item {item.ItemNumber:D2}";
                var status = item.Status;

                if (!AllowedStatuses(item.EvidenceClass).Contains(status))
                {
                    errors.Add($"{label}: {status} is not allowed for {item.EvidenceClass} items.");
                }

                if (
                    string.IsNullOrWhiteSpace(item.StatusLabel)
                    && status is not (TrustIndicatorStatus.QaPending or TrustIndicatorStatus.UnderReview)
                )
                {
                    errors.Add($"{label}: a status label is required.");
                }

                if (
                    status
                        is TrustIndicatorStatus.VerifiedPublicRecord
                            or TrustIndicatorStatus.VerifiedCredential
                )
                {
                    if (!IsHttpsUrl(item.EvidenceUrl))
                    {
                        errors.Add($"{label}: a verified item needs an https evidence URL.");
                    }

                    if (string.IsNullOrWhiteSpace(item.Issuer))
                    {
                        errors.Add($"{label}: a verified item needs an issuer.");
                    }
                }

                if (status == TrustIndicatorStatus.VerifiedCredential && item.IssuedOn == null)
                {
                    errors.Add($"{label}: a completion record needs an issued date.");
                }

                if (
                    item.CtaType == TrustCtaType.ExternalProof
                    && string.IsNullOrWhiteSpace(item.EvidenceUrl)
                )
                {
                    errors.Add($"{label}: an enabled external proof CTA needs an evidence URL.");
                }
                else if (
                    item.CtaType != TrustCtaType.None
                    && !PublicCtaAllowed(item.EvidenceClass, ToValues(item))
                )
                {
                    errors.Add($"{label}: the call to action does not meet the public rules.");
                }

                if (
                    status
                        is TrustIndicatorStatus.Roadmap
                            or TrustIndicatorStatus.QaPending
                            or TrustIndicatorStatus.UnderReview
                    && VerifiedWording.IsMatch(
                        Words(item.Title, item.AltText, item.StatusLabel, item.CtaLabel)
                    )
                )
                {
                    errors.Add(
                        $"{label}: {status} items cannot use verified or certified wording."
                    );
                }

                // A self-assessment completion is not a certification (spec §8, item 02).
                if (
                    item.EvidenceClass == TrustEvidenceClass.VerifiedSelfAssessment
                    && CertifiedWording.IsMatch(
                        Words(
                            item.Title,
                            item.AltText,
                            item.StatusLabel,
                            item.CtaLabel,
                            item.Issuer,
                            item.Scope
                        )
                    )
                )
                {
                    errors.Add(
                        $"{label}: a self-assessment cannot use certified or certification wording."
                    );
                }

                if (item.ExpiresOn is { } expiry && expiry < today)
                {
                    errors.Add(
                        $"{label}: evidence expired on {expiry:yyyy-MM-dd}. Disable it or renew the evidence."
                    );
                }
            }

            return errors;
        }

        /// <summary>Exact name only. For reading the evidence class back from a stored snapshot.</summary>
        public static bool TryParseEvidenceClass(string? raw, out TrustEvidenceClass evidenceClass) =>
            TryParseExact(raw, out evidenceClass);

        /// <summary>Exact enum names only, so a loose parse never accepts a neighbour's value.</summary>
        private static bool TryParseExact<TEnum>(string? raw, out TEnum value)
            where TEnum : struct, Enum
        {
            if (raw != null && Enum.GetNames<TEnum>().Contains(raw, StringComparer.Ordinal))
            {
                value = Enum.Parse<TEnum>(raw);
                return true;
            }

            value = default;
            return false;
        }

        private static string Words(params string?[] values) =>
            string.Join(' ', values.Where(value => !string.IsNullOrWhiteSpace(value)));

        private static string? Trim(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool IsHttpsUrl(string? value) =>
            value != null
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && !string.IsNullOrEmpty(uri.Host);

        private static bool IsRootPath(string? value) =>
            value != null
            && value.StartsWith('/')
            && !value.StartsWith("//", StringComparison.Ordinal)
            && !value.Any(char.IsWhiteSpace);

        private static void AddLengthError(
            List<string> errors,
            string label,
            string? value,
            int max
        )
        {
            if (value != null && value.Length > max)
            {
                errors.Add($"{label} must be {max} characters or fewer.");
            }
        }
    }
}
