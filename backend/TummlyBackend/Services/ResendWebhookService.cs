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

        private static readonly TimeSpan MaxSkew = TimeSpan.FromMinutes(5);

        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<ResendWebhookService> _logger;
        private readonly IGuestInitiatedMarketingWithdrawService _withdraw;
        private readonly IGuestContactSuppressionService _suppressions;
        private readonly Func<DateTime> _utcNow;

        public ResendWebhookService(
            ApplicationDbContext context,
            IOptions<EmailSettings> emailSettings,
            ILogger<ResendWebhookService> logger,
            IGuestInitiatedMarketingWithdrawService withdraw,
            IGuestContactSuppressionService suppressions,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _withdraw = withdraw;
            _suppressions = suppressions;
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

            var suppressionSource = MarketingSuppressionSourceForEvent(type, data);
            if (suppressionSource != null)
            {
                await SuppressEmailMarketingAsync(
                    emailId,
                    data,
                    suppressionSource,
                    ReadEventTime(root) ?? _utcNow(),
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

        private static GuestReplyDeliveryState? ReplyStateForEvent(string? type) =>
            type switch
            {
                EmailDeliveredType => GuestReplyDeliveryState.Delivered,
                EmailBouncedType => GuestReplyDeliveryState.Failed,
                EmailFailedType => GuestReplyDeliveryState.Failed,
                _ => null,
            };

        /// <summary>
        /// Hard bounces and complaints suppress Email marketing. A bounce with no
        /// <c>bounce.type</c> counts as hard. Resend <c>Temporary</c> and
        /// <c>Undetermined</c> bounces do not.
        /// </summary>
        private static string? MarketingSuppressionSourceForEvent(
            string? type,
            JsonElement data
        ) =>
            type switch
            {
                EmailBouncedType when IsHardBounce(data) =>
                    GuestContactSuppressionSources.Bounce,
                EmailComplainedType => GuestContactSuppressionSources.Complaint,
                _ => null,
            };

        private static bool IsHardBounce(JsonElement data)
        {
            if (
                !data.TryGetProperty("bounce", out var bounce)
                || bounce.ValueKind != JsonValueKind.Object
                || !bounce.TryGetProperty("type", out var typeEl)
                || typeEl.ValueKind != JsonValueKind.String
            )
            {
                return true;
            }

            return string.Equals(
                typeEl.GetString()?.Trim(),
                "Permanent",
                StringComparison.OrdinalIgnoreCase
            );
        }

        /// <summary>
        /// Suppresses Email marketing for the guest behind a Campaign delivery. The
        /// restaurant-scoped contact suppression is the same one the unsubscribe form
        /// writes. Events with no Campaign email recipient are logged and skipped.
        /// </summary>
        private async Task SuppressEmailMarketingAsync(
            string? emailId,
            JsonElement data,
            string source,
            DateTime atUtc,
            CancellationToken cancellationToken
        )
        {
            var delivery = await FindDeliveryAsync(emailId, data, cancellationToken);

            LocationGuest? guest = null;
            if (delivery?.LocationGuestId is int locationGuestId)
            {
                guest = await _context.LocationGuests
                    .Include(lg => lg.RestaurantLocation)
                    .Include(lg => lg.MasterGuest)
                    .FirstOrDefaultAsync(
                        lg => lg.Id == locationGuestId,
                        cancellationToken
                    );
            }

            var address = guest?.MasterGuest?.Email ?? guest?.MasterGuest?.NormalizedEmail;
            if (
                guest?.RestaurantLocation == null
                || !GuestContactKey.TryCreate(
                    address,
                    ContactType.Email,
                    out var contactKey
                )
            )
            {
                _logger.LogInformation(
                    "Resend {Source} event has no Campaign email recipient; Email marketing not suppressed (email_id={EmailId}).",
                    source,
                    emailId
                );
                return;
            }

            var restaurantId = guest.RestaurantLocation.RestaurantId;

            // Stage first. WithdrawForLocationGuestAsync saves the staged row with the
            // ledger rows. A repeat event finds the staged row and changes nothing.
            await _suppressions.StageAsync(
                restaurantId,
                contactKey,
                LocationGuestPermissionKind.EmailMarketing,
                atUtc,
                source,
                cancellationToken
            );
            await _withdraw.WithdrawForLocationGuestAsync(
                guest.Id,
                restaurantId,
                LocationGuestPermissionKind.EmailMarketing,
                source,
                cancellationToken
            );
        }

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
