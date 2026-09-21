using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
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
    /// Validates Twilio inbound SMS webhooks and withdraws SmsMarketing on STOP.
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
        private readonly IReadOnlyDictionary<string, int> _inboundByNormalizedTo;
        private readonly IGuestInitiatedMarketingWithdrawService _withdraw;
        private readonly ILogger<TwilioSmsInboundService> _logger;

        public TwilioSmsInboundService(
            IOptions<TwilioSettings> settings,
            IGuestInitiatedMarketingWithdrawService withdraw,
            ILogger<TwilioSmsInboundService> logger
        )
        {
            _settings = settings.Value;
            _withdraw = withdraw;
            _logger = logger;
            _inboundByNormalizedTo = BuildNormalizedInboundMap(
                _settings.InboundNumberRestaurants,
                _settings.ResolvedDefaultRegion
            );

            if (
                !string.IsNullOrWhiteSpace(_settings.AuthToken)
                && _inboundByNormalizedTo.Count == 0
            )
            {
                _logger.LogError(
                    "Twilio AuthToken is set but InboundNumberRestaurants is empty. SMS STOP cannot map To→restaurant; marketing will stay open after STOP."
                );
            }
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

            form.TryGetValue("To", out var toRaw);
            var to = (toRaw ?? string.Empty).Trim();
            if (
                !TryResolveRestaurantId(to, out var restaurantId)
                || restaurantId <= 0
            )
            {
                _logger.LogWarning(
                    "Twilio SMS STOP for unmapped To number {To}; no withdraw.",
                    MaskForLogs(to)
                );
                return TwilioSmsInboundStatus.Accepted;
            }

            form.TryGetValue("From", out var fromRaw);
            if (!TryResolveFromE164(fromRaw, out var fromE164))
            {
                _logger.LogWarning(
                    "Twilio SMS STOP with unparseable From; restaurant {RestaurantId}; no withdraw.",
                    restaurantId
                );
                return TwilioSmsInboundStatus.Accepted;
            }

            await _withdraw.WithdrawForRestaurantContactAsync(
                restaurantId,
                fromE164,
                isEmail: false,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerSources.SmsStop,
                cancellationToken
            );

            return TwilioSmsInboundStatus.Accepted;
        }

        private bool TryResolveRestaurantId(string to, out int restaurantId)
        {
            restaurantId = 0;
            if (string.IsNullOrEmpty(to))
            {
                return false;
            }

            if (
                _settings.InboundNumberRestaurants.TryGetValue(
                    to,
                    out restaurantId
                )
                && restaurantId > 0
            )
            {
                return true;
            }

            foreach (
                var candidate in EnumerateLookupKeys(
                    to,
                    _settings.ResolvedDefaultRegion
                )
            )
            {
                if (
                    _inboundByNormalizedTo.TryGetValue(
                        candidate,
                        out restaurantId
                    )
                    && restaurantId > 0
                )
                {
                    return true;
                }
            }

            restaurantId = 0;
            return false;
        }

        private static IReadOnlyDictionary<string, int> BuildNormalizedInboundMap(
            IReadOnlyDictionary<string, int> configured,
            string defaultRegion
        )
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (key, restaurantId) in configured)
            {
                if (restaurantId <= 0)
                {
                    continue;
                }

                foreach (var lookupKey in EnumerateLookupKeys(key, defaultRegion))
                {
                    map[lookupKey] = restaurantId;
                }
            }

            return map;
        }

        private static IEnumerable<string> EnumerateLookupKeys(
            string raw,
            string defaultRegion
        )
        {
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                yield break;
            }

            yield return trimmed;

            if (TryNormalizeInboundNumber(trimmed, defaultRegion, out var e164))
            {
                if (!string.Equals(e164, trimmed, StringComparison.Ordinal))
                {
                    yield return e164;
                }
            }

            // Digits-only fallback so "+44…" map keys match "4477…" webhook To.
            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (
                digits.Length >= 10
                && !string.Equals(digits, trimmed, StringComparison.Ordinal)
            )
            {
                yield return digits;
            }
        }

        private static bool TryNormalizeInboundNumber(
            string raw,
            string defaultRegion,
            out string e164
        )
        {
            e164 = string.Empty;
            if (
                PhoneNumberHelper.TryNormalizeToE164(
                    raw,
                    defaultRegion,
                    out var normalized
                )
                && !string.IsNullOrEmpty(normalized)
            )
            {
                e164 = normalized;
                return true;
            }

            var trimmed = raw.Trim();
            if (
                trimmed.StartsWith('+')
                && trimmed.Length >= 10
                && trimmed[1..].All(char.IsDigit)
            )
            {
                e164 = trimmed;
                return true;
            }

            // Compact spaced / punctuation forms into +digits when already E.164-ish.
            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (digits.Length >= 10 && trimmed.Contains('+'))
            {
                e164 = $"+{digits}";
                return true;
            }

            return false;
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

        private static string MaskForLogs(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 4)
            {
                return "••••";
            }

            return $"••••{value[^4..]}";
        }
    }
}
