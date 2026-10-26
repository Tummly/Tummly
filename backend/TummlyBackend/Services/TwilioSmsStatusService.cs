using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Twilio.Security;

namespace TummlyBackend.Services
{
    public enum TwilioSmsStatusHandleStatus
    {
        Forbidden,
        Handled,
    }

    /// <summary>
    /// Validates Twilio SMS status callbacks and moves the guest reply with the
    /// matching SID (WS-E R3.3). Repeat and late events never move a final state.
    /// </summary>
    public sealed class TwilioSmsStatusService
    {
        private readonly TwilioSettings _settings;
        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _clock;
        private readonly ILogger<TwilioSmsStatusService> _logger;

        public TwilioSmsStatusService(
            IOptions<TwilioSettings> settings,
            ApplicationDbContext context,
            TimeProvider clock,
            ILogger<TwilioSmsStatusService> logger
        )
        {
            _settings = settings.Value;
            _context = context;
            _clock = clock;
            _logger = logger;
        }

        public async Task<TwilioSmsStatusHandleStatus> HandleAsync(
            string requestUrl,
            IReadOnlyDictionary<string, string> form,
            string? twilioSignature,
            CancellationToken cancellationToken = default
        )
        {
            if (!IsSignatureValid(requestUrl, form, twilioSignature))
            {
                return TwilioSmsStatusHandleStatus.Forbidden;
            }

            form.TryGetValue("MessageSid", out var sidRaw);
            form.TryGetValue("MessageStatus", out var statusRaw);
            var sid = (sidRaw ?? string.Empty).Trim();
            var next = ReplyStateForStatus(statusRaw);
            if (sid.Length == 0 || next == null)
            {
                return TwilioSmsStatusHandleStatus.Handled;
            }

            var replies = await _context.FeedbackGuestResponses
                .Where(row => row.ProviderMessageId == sid)
                .ToListAsync(cancellationToken);
            if (replies.Count == 0)
            {
                // Campaign SMS and other sends are not guest replies.
                _logger.LogInformation(
                    "Twilio SMS status with no matching guest reply (sid={MessageSid}).",
                    sid
                );
                return TwilioSmsStatusHandleStatus.Handled;
            }

            var atUtc = _clock.GetUtcNow().UtcDateTime;
            foreach (var reply in replies)
            {
                GuestReplyDeliveryStateMachine.TryAdvance(reply, next.Value, atUtc);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return TwilioSmsStatusHandleStatus.Handled;
        }

        /// <summary>
        /// <c>delivered</c> → Delivered; <c>undelivered</c> and <c>failed</c> → Failed;
        /// anything else (queued, sending, sent, accepted) → no change.
        /// </summary>
        private static GuestReplyDeliveryState? ReplyStateForStatus(string? status) =>
            (status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "delivered" => GuestReplyDeliveryState.Delivered,
                "undelivered" => GuestReplyDeliveryState.Failed,
                "failed" => GuestReplyDeliveryState.Failed,
                _ => null,
            };

        private bool IsSignatureValid(
            string requestUrl,
            IReadOnlyDictionary<string, string> form,
            string? twilioSignature
        )
        {
            if (
                string.IsNullOrWhiteSpace(_settings.AuthToken)
                || string.IsNullOrWhiteSpace(twilioSignature)
                || string.IsNullOrWhiteSpace(requestUrl)
            )
            {
                return false;
            }

            var parameters = form.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal
            );
            var validator = new RequestValidator(_settings.AuthToken);
            return validator.Validate(requestUrl, parameters, twilioSignature);
        }
    }
}
