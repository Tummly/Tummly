using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Verifies Resend/Svix signatures and records first email open on
    /// <see cref="Models.CampaignRecipientDelivery"/>.
    /// </summary>
    public sealed class ResendWebhookService : IResendWebhookService
    {
        public const string EmailOpenedType = "email.opened";
        public const string EmailDeliveredType = "email.delivered";
        public const string EmailBouncedType = "email.bounced";
        public const string EmailFailedType = "email.failed";
        public const string EmailComplainedType = "email.complained";

        /// <summary>Resend bounce.type for a hard (permanent) bounce.</summary>
        public const string PermanentBounceType = "Permanent";

        private static readonly TimeSpan MaxSkew = TimeSpan.FromMinutes(5);

        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<ResendWebhookService> _logger;
        private readonly IGuestInitiatedMarketingWithdrawService _withdraw;
        private readonly Func<DateTime> _utcNow;

        public ResendWebhookService(
            ApplicationDbContext context,
            IOptions<EmailSettings> emailSettings,
            ILogger<ResendWebhookService> logger,
            IGuestInitiatedMarketingWithdrawService withdraw,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _withdraw = withdraw;
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

            if (!root.TryGetProperty("data", out var data))
            {
                return ResendWebhookHandleStatus.Accepted;
            }

            var emailId = data.TryGetProperty("email_id", out var emailIdEl)
                ? emailIdEl.GetString()?.Trim()
                : null;

            if (IsSuppressingEvent(type, data))
            {
                await ApplyEmailSuppressionAsync(
                    type!,
                    emailId,
                    data,
                    cancellationToken
                );
            }

            var replyState = ReplyStateForEvent(type);
            if (replyState != null)
            {
                await ApplyReplyDeliveryAsync(
                    emailId,
                    replyState.Value,
                    ReadEventTime(root) ?? _utcNow(),
                    cancellationToken
                );
                return ResendWebhookHandleStatus.Accepted;
            }

            if (!string.Equals(type, EmailOpenedType, StringComparison.Ordinal))
            {
                // Acknowledge unknown events so Resend does not retry forever.
                return ResendWebhookHandleStatus.Accepted;
            }

            var openedAt = ReadEventTime(root) ?? _utcNow();

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

        private static bool IsSuppressingEvent(string? type, JsonElement data)
        {
            if (string.Equals(type, EmailComplainedType, StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(type, EmailBouncedType, StringComparison.Ordinal)
                && data.TryGetProperty("bounce", out var bounce)
                && bounce.ValueKind == JsonValueKind.Object
                && bounce.TryGetProperty("type", out var bounceType)
                && string.Equals(
                    bounceType.GetString(),
                    PermanentBounceType,
                    StringComparison.Ordinal
                );
        }

        /// <summary>
        /// Hard bounce and complaint withdraw EmailMarketing for the recipient at
        /// the restaurant that sent the email, and stage the restaurant-scoped
        /// suppression so later sends and re-submits stay withdrawn.
        /// </summary>
        private async Task ApplyEmailSuppressionAsync(
            string type,
            string? emailId,
            JsonElement data,
            CancellationToken cancellationToken
        )
        {
            var recipient = ReadFirstRecipient(data);
            if (string.IsNullOrWhiteSpace(emailId) || recipient == null)
            {
                return;
            }

            var restaurantId = await ResolveRestaurantIdAsync(
                emailId,
                cancellationToken
            );
            if (restaurantId == null)
            {
                _logger.LogInformation(
                    "Resend {EventType} with no matching sent email (email_id={EmailId}).",
                    type,
                    emailId
                );
                return;
            }

            var source = string.Equals(
                type,
                EmailComplainedType,
                StringComparison.Ordinal
            )
                ? GuestContactSuppressionSources.EmailComplaint
                : GuestContactSuppressionSources.EmailBounce;

            await _withdraw.WithdrawForRestaurantContactAsync(
                restaurantId.Value,
                recipient,
                isEmail: true,
                LocationGuestPermissionKind.EmailMarketing,
                source == GuestContactSuppressionSources.EmailComplaint
                    ? LocationGuestPermissionLedgerSources.EmailComplaint
                    : LocationGuestPermissionLedgerSources.EmailBounce,
                cancellationToken,
                suppressionSource: source
            );
        }

        private static string? ReadFirstRecipient(JsonElement data)
        {
            if (
                !data.TryGetProperty("to", out var to)
                || to.ValueKind != JsonValueKind.Array
            )
            {
                return null;
            }

            foreach (var item in to.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var address = item.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(address))
                {
                    return address;
                }
            }

            return null;
        }

        /// <summary>
        /// Restaurant that sent the email: Campaign delivery (guest location)
        /// first, then Feedback guest reply.
        /// </summary>
        private async Task<int?> ResolveRestaurantIdAsync(
            string emailId,
            CancellationToken cancellationToken
        )
        {
            var campaignRestaurantId = await _context.CampaignRecipientDeliveries
                .Where(row => row.ProviderMessageId == emailId)
                .Select(row => row.LocationGuest!.RestaurantLocation!.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (campaignRestaurantId > 0)
            {
                return campaignRestaurantId;
            }

            var replyRestaurantId = await _context.FeedbackGuestResponses
                .Where(row => row.ProviderMessageId == emailId)
                .Select(row => row.Feedback!.RestaurantLocation!.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (replyRestaurantId > 0)
            {
                return replyRestaurantId;
            }

            return null;
        }

        private static GuestReplyDeliveryState? ReplyStateForEvent(string? type) =>
            type switch
            {
                EmailDeliveredType => GuestReplyDeliveryState.Delivered,
                EmailBouncedType => GuestReplyDeliveryState.Failed,
                EmailFailedType => GuestReplyDeliveryState.Failed,
                _ => null,
            };

        /// <summary>
        /// Moves guest replies with this provider email id to <paramref name="next"/>.
        /// <see cref="GuestReplyDeliveryStateMachine"/> keeps the first final state.
        /// </summary>
        private async Task ApplyReplyDeliveryAsync(
            string? emailId,
            GuestReplyDeliveryState next,
            DateTime atUtc,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(emailId))
            {
                return;
            }

            var replies = await _context.FeedbackGuestResponses
                .Where(row => row.ProviderMessageId == emailId)
                .ToListAsync(cancellationToken);
            if (replies.Count == 0)
            {
                _logger.LogInformation(
                    "Resend delivery event with no matching guest reply (email_id={EmailId}).",
                    emailId
                );
                return;
            }

            foreach (var reply in replies)
            {
                GuestReplyDeliveryStateMachine.TryAdvance(reply, next, atUtc);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static DateTime? ReadEventTime(JsonElement root)
        {
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
                return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            return null;
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
