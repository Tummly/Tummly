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

        private static readonly Regex VerifiedWording = new(
            @"\b(verified|certified)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );

        public static DateOnly LondonToday(DateTime utcNow) =>
            DateOnly.FromDateTime(LondonDateFormat.ToLondonLocal(utcNow));

        /// <summary>Enabled and not past its expiry date (the expiry day itself is still live).</summary>
        public static bool IsLive(bool enabled, DateOnly? expiresOn, DateOnly today) =>
            enabled && (expiresOn is not { } expiry || expiry >= today);

        public static TrustIndicatorEditableValues ToValues(TrustIndicator row) =>
            new()
            {
                Enabled = row.Enabled,
                CtaType = row.CtaType.ToString(),
                CtaTarget = row.CtaTarget,
                EvidenceUrl = row.EvidenceUrl,
                ImageUrl = row.ImageUrl,
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
            row.CtaType = Enum.Parse<TrustCtaType>(values.CtaType!);
            row.CtaTarget = values.CtaTarget;
            row.EvidenceUrl = values.EvidenceUrl;
            row.ImageUrl = values.ImageUrl;
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
            TrustIndicatorEditableValues input,
            List<string> errors
        )
        {
            // Exact names only: Enum.TryParse would also accept "ExternalProof, None".
            var ctaTypeRaw = input.CtaType?.Trim() ?? string.Empty;
            var ctaTypeKnown = Enum.GetNames<TrustCtaType>()
                .Contains(ctaTypeRaw, StringComparer.Ordinal);
            if (!ctaTypeKnown)
            {
                errors.Add("CTA type must be None, ExternalProof or InternalPage.");
            }

            var ctaType = ctaTypeKnown
                ? Enum.Parse<TrustCtaType>(ctaTypeRaw)
                : TrustCtaType.None;

            var ctaTarget = Trim(input.CtaTarget);
            var evidenceUrl = Trim(input.EvidenceUrl);
            var imageUrl = Trim(input.ImageUrl);
            var iconKey = Trim(input.IconKey);
            var altText = Trim(input.AltText);
            var tooltip = Trim(input.Tooltip);
            var issuer = Trim(input.Issuer);
            var scope = Trim(input.Scope);

            if (ctaType == TrustCtaType.None)
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

            if (imageUrl != null && !IsHttpsUrl(imageUrl) && !IsRootPath(imageUrl))
            {
                errors.Add("Image must be an https link or a path that starts with /.");
            }

            if (imageUrl != null && altText == null)
            {
                errors.Add("Alt text is required when an image is set.");
            }

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
                CtaType = ctaType.ToString(),
                CtaTarget = ctaTarget,
                EvidenceUrl = evidenceUrl,
                ImageUrl = imageUrl,
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

        /// <summary>Publish gate (ADM-15-05, 15-08). Empty list means the draft may publish.</summary>
        public static IReadOnlyList<string> PublishGateErrors(
            IReadOnlyList<TrustIndicator> items,
            DateOnly today
        )
        {
            var errors = new List<string>();
            foreach (var item in items.OrderBy(row => row.ItemNumber))
            {
                var label = $"Item {item.ItemNumber:D2}";

                if (
                    item.Enabled
                    && item.CtaType == TrustCtaType.ExternalProof
                    && string.IsNullOrWhiteSpace(item.EvidenceUrl)
                )
                {
                    errors.Add($"{label}: an enabled external proof CTA needs an evidence URL.");
                }

                if (
                    item.EvidenceClass
                        is TrustEvidenceClass.Roadmap
                            or TrustEvidenceClass.OwnControlQaPending
                )
                {
                    if (item.CtaType != TrustCtaType.None)
                    {
                        errors.Add($"{label}: {item.EvidenceClass} items cannot have a CTA.");
                    }

                    if (
                        VerifiedWording.IsMatch(
                            $"{item.Title} {item.AltText} {item.Tooltip} {item.Issuer} {item.Scope}"
                        )
                    )
                    {
                        errors.Add(
                            $"{label}: {item.EvidenceClass} items cannot use verified or certified wording."
                        );
                    }
                }

                if (item.Enabled && item.ExpiresOn is { } expiry && expiry < today)
                {
                    errors.Add(
                        $"{label}: evidence expired on {expiry:yyyy-MM-dd}. Disable it or renew the evidence."
                    );
                }
            }

            return errors;
        }

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
