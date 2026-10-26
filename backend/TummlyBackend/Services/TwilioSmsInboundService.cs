using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using Twilio.Security;

namespace TummlyBackend.Services
{
    public enum TwilioSmsInboundStatus
    {
        Forbidden,
        Accepted,
    }

    /// <summary>
    /// Validates Twilio inbound SMS webhooks. On a STOP keyword, the sender's phone
    /// is stopped at every restaurant that texted it (shared-number rule A, R4.3).
    /// The To number is not used: one shared number serves all restaurants.
    /// </summary>
    public sealed class TwilioSmsInboundService
    {
        private static readonly HashSet<string> StopKeywords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "STOP",
                "STOPALL",
                "UNSUBSCRIBE",
                "CANCEL",
                "END",
                "QUIT",
            };

        private readonly TwilioSettings _settings;
        private readonly ApplicationDbContext _context;
        private readonly IGuestInitiatedMarketingWithdrawService _withdraw;
        private readonly ILogger<TwilioSmsInboundService> _logger;

        public TwilioSmsInboundService(
            IOptions<TwilioSettings> settings,
            ApplicationDbContext context,
            IGuestInitiatedMarketingWithdrawService withdraw,
            ILogger<TwilioSmsInboundService> logger
        )
        {
            _settings = settings.Value;
            _context = context;
            _withdraw = withdraw;
            _logger = logger;
        }

        public async Task<TwilioSmsInboundStatus> HandleAsync(
            string requestUrl,
            IReadOnlyDictionary<string, string> form,
            string? twilioSignature,
            CancellationToken cancellationToken = default
        )
        {
            if (!IsSignatureValid(requestUrl, form, twilioSignature))
            {
                return TwilioSmsInboundStatus.Forbidden;
            }

            form.TryGetValue("Body", out var bodyRaw);
            var body = (bodyRaw ?? string.Empty).Trim();
            if (!StopKeywords.Contains(body))
            {
                return TwilioSmsInboundStatus.Accepted;
            }

            form.TryGetValue("From", out var fromRaw);
            if (!TryResolveFromE164(fromRaw, out var fromE164))
            {
                _logger.LogWarning(
                    "Twilio SMS STOP with unparseable From; no change."
                );
                return TwilioSmsInboundStatus.Accepted;
            }

            await StopTextedRestaurantsAsync(fromE164, cancellationToken);

            return TwilioSmsInboundStatus.Accepted;
        }

        /// <summary>
        /// Finds the guest rows that match the phone. For each restaurant that sent
        /// an SMS to one of them, withdraws SMS marketing and sets SmsStoppedAtUtc.
        /// </summary>
        private async Task StopTextedRestaurantsAsync(
            string fromE164,
            CancellationToken cancellationToken
        )
        {
            var inputDigits = new string(fromE164.Where(char.IsDigit).ToArray());
            var last9 = inputDigits.Length >= 9 ? inputDigits[^9..] : inputDigits;

            var candidates = (
                await _context.MasterGuests
                    .Where(master =>
                        master.NormalizedPhone != null
                        && master.NormalizedPhone.EndsWith(last9)
                    )
                    .ToListAsync(cancellationToken)
            )
                .Where(master =>
                    GuestInitiatedMarketingWithdrawService.MasterGuestMatchesE164(
                        master,
                        fromE164
                    )
                )
                .ToList();

            var textedMasterIds = await SmsSentMasterGuestIdsAsync(
                candidates.Select(master => master.Id).ToList(),
                cancellationToken
            );
            var textedRestaurantIds = candidates
                .Where(master => textedMasterIds.Contains(master.Id))
                .Select(master => master.RestaurantId)
                .ToHashSet();

            if (textedRestaurantIds.Count == 0)
            {
                _logger.LogInformation(
                    "Twilio SMS STOP matched no restaurant that texted this number. MaskedPhone {MaskedPhone}; no change.",
                    LogContactMask.Phone(fromE164)
                );
                return;
            }

            var stoppedAtUtc = DateTime.UtcNow;
            foreach (
                var master in candidates.Where(master =>
                    textedRestaurantIds.Contains(master.RestaurantId)
                )
            )
            {
                master.SmsStoppedAtUtc ??= stoppedAtUtc;
            }

            foreach (var restaurantId in textedRestaurantIds)
            {
                await _withdraw.WithdrawForRestaurantContactAsync(
                    restaurantId,
                    fromE164,
                    isEmail: false,
                    LocationGuestPermissionKind.SmsMarketing,
                    LocationGuestPermissionLedgerSources.SmsStop,
                    cancellationToken
                );
            }
        }

        /// <summary>
        /// MasterGuest ids that received an SMS: a guest reply (channel sms) or an
        /// accepted campaign SMS send.
        /// </summary>
        private async Task<HashSet<int>> SmsSentMasterGuestIdsAsync(
            List<int> masterGuestIds,
            CancellationToken cancellationToken
        )
        {
            var repliedIds = await _context.FeedbackGuestResponses
                .Where(response =>
                    response.Channel == FeedbackGuestResponseChannel.Sms
                    && response.Feedback!.LocationGuest != null
                    && masterGuestIds.Contains(
                        response.Feedback.LocationGuest.MasterGuestId
                    )
                )
                .Select(response => response.Feedback!.LocationGuest!.MasterGuestId)
                .ToListAsync(cancellationToken);

            var campaignIds = await _context.CampaignRecipientDeliveries
                .Where(delivery =>
                    delivery.Channel == "sms"
                    && delivery.AcceptedAtUtc != null
                    && delivery.LocationGuest != null
                    && masterGuestIds.Contains(delivery.LocationGuest.MasterGuestId)
                )
                .Select(delivery => delivery.LocationGuest!.MasterGuestId)
                .ToListAsync(cancellationToken);

            return repliedIds.Concat(campaignIds).ToHashSet();
        }

        private bool TryResolveFromE164(string? fromRaw, out string fromE164)
        {
            fromE164 = string.Empty;
            if (
                PhoneNumberHelper.TryNormalizeToE164(
                    fromRaw ?? string.Empty,
                    _settings.ResolvedDefaultRegion,
                    out var normalized
                )
                && !string.IsNullOrEmpty(normalized)
            )
            {
                fromE164 = normalized;
                return true;
            }

            // Twilio sends E.164 From; accept +digits when libphonenumber rejects
            // (e.g. reserved test ranges) so STOP still withdraws.
            var trimmed = (fromRaw ?? string.Empty).Trim();
            if (
                trimmed.StartsWith('+')
                && trimmed.Length >= 10
                && trimmed[1..].All(char.IsDigit)
            )
            {
                fromE164 = trimmed;
                return true;
            }

            return false;
        }

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
            return validator.Validate(
                requestUrl,
                parameters,
                twilioSignature
            );
        }
    }
}
