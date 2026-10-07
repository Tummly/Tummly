using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Verifies Resend/Svix signatures and records first email open on
    /// <see cref="Models.CampaignRecipientDelivery"/>.
    /// </summary>
    public sealed class ResendWebhookService : IResendWebhookService
    {
        public const string EmailOpenedType = "email.opened";

        private static readonly TimeSpan MaxSkew = TimeSpan.FromMinutes(5);

        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<ResendWebhookService> _logger;
        private readonly Func<DateTime> _utcNow;

        public ResendWebhookService(
            ApplicationDbContext context,
            IOptions<EmailSettings> emailSettings,
            ILogger<ResendWebhookService> logger,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<ResendWebhookHandleStatus> HandleAsync(
            string rawBody,
            string? svixId,
            string? svixTimestamp,
            string? svixSignature,
            CancellationToken cancellationToken = default
        )
        {
            var secret = _emailSettings.WebhookSigningSecret?.Trim();
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning(
                    "Resend webhook rejected — EmailSettings:WebhookSigningSecret is empty."
                );
                return ResendWebhookHandleStatus.Misconfigured;
            }

            if (
                !IsSignatureValid(
                    rawBody,
                    svixId,
                    svixTimestamp,
                    svixSignature,
                    secret,
                    _utcNow()
                )
            )
            {
                return ResendWebhookHandleStatus.BadSignature;
            }

            using var document = JsonDocument.Parse(
                string.IsNullOrEmpty(rawBody) ? "{}" : rawBody
            );
            var root = document.RootElement;
            var type = root.TryGetProperty("type", out var typeEl)
                ? typeEl.GetString()?.Trim()
                : null;

            if (!string.Equals(type, EmailOpenedType, StringComparison.Ordinal))
            {
                // Acknowledge unknown events so Resend does not retry forever.
                return ResendWebhookHandleStatus.Accepted;
            }

            if (!root.TryGetProperty("data", out var data))
            {
                return ResendWebhookHandleStatus.Accepted;
            }

            var emailId = data.TryGetProperty("email_id", out var emailIdEl)
                ? emailIdEl.GetString()?.Trim()
                : null;

            DateTime? openedAt = null;
            if (
                root.TryGetProperty("created_at", out var createdEl)
                && createdEl.ValueKind == JsonValueKind.String
                && DateTime.TryParse(
                    createdEl.GetString(),
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var parsed
                )
            )
            {
                openedAt = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            openedAt ??= _utcNow();

            var delivery = await FindDeliveryAsync(
                emailId,
                data,
                cancellationToken
            );
            if (delivery == null)
            {
                _logger.LogInformation(
                    "Resend email.opened with no matching Campaign delivery (email_id={EmailId}).",
                    emailId
                );
                return ResendWebhookHandleStatus.Accepted;
            }

            if (delivery.OpenedAtUtc != null)
            {
                return ResendWebhookHandleStatus.Accepted;
            }

            delivery.OpenedAtUtc = openedAt;
            delivery.UpdatedAtUtc = _utcNow();
            await _context.SaveChangesAsync(cancellationToken);
            return ResendWebhookHandleStatus.Accepted;
        }

        private async Task<Models.CampaignRecipientDelivery?> FindDeliveryAsync(
            string? emailId,
            JsonElement data,
            CancellationToken cancellationToken
        )
        {
            if (!string.IsNullOrWhiteSpace(emailId))
            {
                var byId = await _context.CampaignRecipientDeliveries
                    .FirstOrDefaultAsync(
                        row => row.ProviderMessageId == emailId,
                        cancellationToken
                    );
                if (byId != null)
                {
                    return byId;
                }
            }

            if (
                !data.TryGetProperty("tags", out var tags)
                || tags.ValueKind != JsonValueKind.Object
            )
            {
                return null;
            }

            if (
                !TryReadPositiveIntTag(tags, "campaign_id", out var campaignId)
                || !TryReadPositiveIntTag(
                    tags,
                    "location_guest_id",
                    out var locationGuestId
                )
            )
            {
                return null;
            }

            return await _context.CampaignRecipientDeliveries
                .FirstOrDefaultAsync(
                    row =>
                        row.CampaignId == campaignId
                        && row.LocationGuestId == locationGuestId
                        && row.Channel == "email",
                    cancellationToken
                );
        }

        private static bool TryReadPositiveIntTag(
            JsonElement tags,
            string name,
            out int value
        )
        {
            value = 0;
            if (
                !tags.TryGetProperty(name, out var el)
                || el.ValueKind != JsonValueKind.String
            )
            {
                return false;
            }

            return int.TryParse(el.GetString(), out value) && value > 0;
        }

        /// <summary>
        /// Svix / Standard Webhooks verify — used by Resend.
        /// </summary>
        public static bool IsSignatureValid(
            string rawBody,
            string? svixId,
            string? svixTimestamp,
            string? svixSignature,
            string signingSecret,
            DateTime utcNow
        )
        {
            if (
                string.IsNullOrWhiteSpace(svixId)
                || string.IsNullOrWhiteSpace(svixTimestamp)
                || string.IsNullOrWhiteSpace(svixSignature)
            )
            {
                return false;
            }

            if (
                !long.TryParse(svixTimestamp.Trim(), out var unixSeconds)
            )
            {
                return false;
            }

            var eventTime = DateTimeOffset
                .FromUnixTimeSeconds(unixSeconds)
                .UtcDateTime;
            if (Math.Abs((utcNow - eventTime).TotalSeconds) > MaxSkew.TotalSeconds)
            {
                return false;
            }

            byte[] secretBytes;
            try
            {
                var key = signingSecret.Trim();
                if (key.StartsWith("whsec_", StringComparison.Ordinal))
                {
                    key = key["whsec_".Length..];
                }

                secretBytes = Convert.FromBase64String(key);
            }
            catch (FormatException)
            {
                return false;
            }

            var signedContent = $"{svixId.Trim()}.{svixTimestamp.Trim()}.{rawBody}";
            var expected = Convert.ToBase64String(
                HMACSHA256.HashData(
                    secretBytes,
                    Encoding.UTF8.GetBytes(signedContent)
                )
            );

            foreach (
                var part in svixSignature.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries
                )
            )
            {
                var comma = part.IndexOf(',');
                if (comma <= 0 || comma >= part.Length - 1)
                {
                    continue;
                }

                var version = part[..comma];
                var sig = part[(comma + 1)..];
                if (
                    !string.Equals(version, "v1", StringComparison.Ordinal)
                )
                {
                    continue;
                }

                if (SecureEquals(sig, expected))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SecureEquals(string left, string right)
        {
            var a = Encoding.UTF8.GetBytes(left);
            var b = Encoding.UTF8.GetBytes(right);
            if (a.Length != b.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
