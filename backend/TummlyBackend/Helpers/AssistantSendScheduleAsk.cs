using System.Globalization;
using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Later Refuse send / schedule ask: land rules only. Does not send.
    /// </summary>
    public static partial class AssistantSendScheduleAsk
    {
        public const string KindCampaign = "campaign";
        public const string KindRecovery = "recovery";
        public const string StepReview = "review";
        public const string StepSchedule = "schedule";
        public const string ModeSendNow = "send-now";
        public const string ModeScheduleLater = "schedule-later";

        public readonly record struct CampaignLandingResult(
            string Step,
            string? ScheduleMode,
            string? DateLocal,
            string? TimeLocal
        );

        public static bool LooksLikeSendOrSchedule(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (LooksLikeSendNow(lower))
            {
                return true;
            }

            if (LooksLikeSendThisCampaign(lower))
            {
                return true;
            }

            return HasScheduleWord(lower);
        }

        public static bool LooksLikeOfferActivate(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return ContainsAny(
                lower,
                "activate this offer",
                "activate the offer",
                "activate an offer",
                "activate it",
                "activate offer",
                "issue an offer",
                "issue the offer",
                "issue this offer",
                "issue it",
                "issue offer",
                "redeem the offer",
                "redeem this offer",
                "redeem an offer",
                "redeem it",
                "redeem offer"
            );
        }

        public static bool LooksLikeTimedSchedule(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (!HasScheduleWord(lower))
            {
                return false;
            }

            return ContainsAny(
                    lower,
                    " for ",
                    " at ",
                    "tomorrow",
                    "later",
                    "friday",
                    "monday",
                    "tuesday",
                    "wednesday",
                    "thursday",
                    "saturday",
                    "sunday"
                )
                || DateTimeRegex().IsMatch(message)
                || NamedDateRegex().IsMatch(message);
        }

        public static bool LooksLikeRecoverySend(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return ContainsAny(
                lower,
                "send the recovery",
                "send this recovery",
                "send the response",
                "send this response",
                "send the guest"
            );
        }

        public static bool LooksLikeCampaignNamed(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            return lower.Contains("campaign", StringComparison.Ordinal);
        }

        public static CampaignLandingResult CampaignLanding(string message, DateTime nowUtc)
        {
            var lower = message.Trim().ToLowerInvariant();
            var sendNow = LooksLikeSendNow(lower);
            var schedule = HasScheduleWord(lower);
            if (IsAmbiguous(lower, sendNow, schedule))
            {
                return new CampaignLandingResult(StepSchedule, ModeScheduleLater, null, null);
            }

            if ((sendNow || LooksLikeSendThisCampaign(lower)) && !schedule)
            {
                return new CampaignLandingResult(StepReview, ModeSendNow, null, null);
            }

            var parsed = TryParseFutureDatetime(message, nowUtc);
            if (parsed is { } at)
            {
                return new CampaignLandingResult(
                    StepReview,
                    ModeScheduleLater,
                    at.DateLocal,
                    at.TimeLocal
                );
            }

            return new CampaignLandingResult(
                StepSchedule,
                ModeScheduleLater,
                null,
                null
            );
        }

        public static bool IsNamedCampaignMismatch(string message, string storedCampaignName)
        {
            var named = NamedCampaignRegex().Match(message);
            if (!named.Success)
            {
                return false;
            }

            var fragment = named.Groups[1].Value.Trim();
            if (fragment.Length == 0)
            {
                return false;
            }

            if (fragment.Equals("this", StringComparison.OrdinalIgnoreCase)
                || fragment.Equals("the", StringComparison.OrdinalIgnoreCase)
                || fragment.Equals("that", StringComparison.OrdinalIgnoreCase)
                || fragment.Equals("my", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var stored = storedCampaignName.Trim();
            if (stored.Length == 0)
            {
                return true;
            }

            return stored.Contains(fragment, StringComparison.OrdinalIgnoreCase) == false
                && fragment.Contains(stored, StringComparison.OrdinalIgnoreCase) == false;
        }

        private static bool IsAmbiguous(string lower, bool sendNow, bool schedule)
            => (sendNow && schedule)
                || (lower.Contains("send or schedule", StringComparison.Ordinal)
                    || lower.Contains("schedule or send", StringComparison.Ordinal));

        private static bool LooksLikeSendNow(string lower)
            => ContainsAny(lower, "send it now", "send this now", "send now");

        private static bool LooksLikeSendThisCampaign(string lower)
            => ContainsAny(
                lower,
                "send this campaign",
                "send the campaign",
                "send this draft",
                "send the draft"
            );

        private static bool HasScheduleWord(string lower)
            => ScheduleWordRegex().IsMatch(lower);

        private static (string DateLocal, string TimeLocal)? TryParseFutureDatetime(
            string message,
            DateTime nowUtc
        )
        {
            var named = NamedDateTimeRegex().Match(message);
            if (named.Success)
            {
                return ParseNamed(named.Groups[1].Value, named.Groups[2].Value, nowUtc);
            }

            var dateOnly = NamedDateRegex().Match(message);
            if (dateOnly.Success)
            {
                return null;
            }

            var iso = IsoDateTimeRegex().Match(message);
            if (iso.Success
                && DateTime.TryParse(
                    iso.Value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var isoAt
                ))
            {
                return FutureOrNull(isoAt, nowUtc);
            }

            return null;
        }

        private static (string DateLocal, string TimeLocal)? ParseNamed(
            string dateText,
            string timeText,
            DateTime nowUtc
        )
        {
            if (!DateTime.TryParseExact(
                    $"{dateText.Trim()} {timeText.Trim()}",
                    [
                        "d MMMM yyyy HH:mm",
                        "d MMMM yyyy H:mm",
                        "dd MMMM yyyy HH:mm",
                        "d MMM yyyy HH:mm",
                        "dd MMM yyyy HH:mm",
                    ],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var at
                ))
            {
                return null;
            }

            return FutureOrNull(at, nowUtc);
        }

        private static (string DateLocal, string TimeLocal)? FutureOrNull(
            DateTime atLocal,
            DateTime nowUtc
        )
        {
            var utc = nowUtc.Kind == DateTimeKind.Utc
                ? nowUtc
                : DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(utc, ScheduleTimeZone);
            if (atLocal <= nowLocal)
            {
                return null;
            }

            return (atLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                atLocal.ToString("HH:mm", CultureInfo.InvariantCulture));
        }

        private static readonly TimeZoneInfo ScheduleTimeZone = ResolveScheduleTimeZone();

        private static TimeZoneInfo ResolveScheduleTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            }
        }

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));

        [GeneratedRegex(@"\bschedule\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ScheduleWordRegex();

        [GeneratedRegex(
            @"\bthe\s+(.+?)\s+campaign\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex NamedCampaignRegex();

        [GeneratedRegex(
            @"\b(\d{1,2}\s+[A-Za-z]+\s+\d{4})\s+at\s+(\d{1,2}:\d{2})\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex NamedDateTimeRegex();

        [GeneratedRegex(
            @"\b\d{1,2}\s+[A-Za-z]+\s+\d{4}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex NamedDateRegex();

        [GeneratedRegex(
            @"\b\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}\b",
            RegexOptions.CultureInvariant
        )]
        private static partial Regex IsoDateTimeRegex();

        [GeneratedRegex(
            @"\b\d{4}-\d{2}-\d{2}\b|\b\d{1,2}\s+[A-Za-z]+\s+\d{4}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        )]
        private static partial Regex DateTimeRegex();
    }
}
