namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned Gap when a create binds a channel with zero eligible guests.
    /// Prevents freeform Clarify / ordinal replies from dropping the named channel.
    /// </summary>
    public enum AssistantEmptyChannelAudienceChoice
    {
        SwitchChannel,
        BroadenToEmail,
        Wait,
        CreateAnyway,
    }

    public static class AssistantEmptyChannelAudience
    {
        public const string OptionUseEmail = "Use Email instead";
        public const string OptionBroaden =
            "Broaden to all marketing-eligible guests";
        public const string OptionWaitSms = "Wait for SMS opt-ins";
        public const string OptionCreateSmsAnyway = "Create SMS draft anyway";

        public const string OptionUseSms = "Use SMS instead";
        public const string OptionWaitEmail = "Wait for Email opt-ins";
        public const string OptionCreateEmailAnyway = "Create Email draft anyway";

        public static readonly IReadOnlyList<string> EmptySmsOptions =
        [
            OptionUseEmail,
            OptionBroaden,
            OptionWaitSms,
            OptionCreateSmsAnyway,
        ];

        public static readonly IReadOnlyList<string> EmptyEmailOptions =
        [
            OptionUseSms,
            OptionBroaden,
            OptionWaitEmail,
            OptionCreateEmailAnyway,
        ];

        public static string BodyForEmptySms(
            string locationName,
            int emailEligibleCount
        )
        {
            var emailLine = emailEligibleCount > 0
                ? $"There are, however, {emailEligibleCount} guest"
                    + (emailEligibleCount == 1 ? "" : "s")
                    + " marked eligible for Email."
                : "There are also no Email-eligible guests at this location.";

            return
                $"I checked guests for “{locationName}” and found no guests who "
                + "are SMS-marketing eligible. I can’t create a meaningful SMS "
                + "campaign because the audience would be empty. "
                + emailLine
                + "\n\nHow would you like to proceed?\n\n"
                + "Options:\n"
                + $"1) {OptionUseEmail} (send by Email).\n"
                + $"2) {OptionBroaden} (any channel they’ve opted into).\n"
                + $"3) {OptionWaitSms} before creating the SMS campaign.\n"
                + $"4) {OptionCreateSmsAnyway} (it will have no recipients "
                + "until there are SMS opt-ins).\n\n"
                + "Tell me which option you prefer.";
        }

        public static string BodyForEmptyEmail(
            string locationName,
            int smsEligibleCount
        )
        {
            var smsLine = smsEligibleCount > 0
                ? $"There are, however, {smsEligibleCount} guest"
                    + (smsEligibleCount == 1 ? "" : "s")
                    + " marked eligible for SMS."
                : "There are also no SMS-eligible guests at this location.";

            return
                $"I checked guests for “{locationName}” and found no guests who "
                + "are Email-marketing eligible. I can’t create a meaningful Email "
                + "campaign because the audience would be empty. "
                + smsLine
                + "\n\nHow would you like to proceed?\n\n"
                + "Options:\n"
                + $"1) {OptionUseSms} (send by SMS).\n"
                + $"2) {OptionBroaden} (any channel they’ve opted into).\n"
                + $"3) {OptionWaitEmail} before creating the Email campaign.\n"
                + $"4) {OptionCreateEmailAnyway} (it will have no recipients "
                + "until there are Email opt-ins).\n\n"
                + "Tell me which option you prefer.";
        }

        public static string WaitBody(string channelLabel)
            => $"Understood. I did not save a Campaign Draft. "
                + $"Collect {channelLabel} opt-ins first, then ask again.";

        public static AssistantEmptyChannelAudienceChoice? ResolveChoice(
            string emptyChannelId,
            string chosenOption
        )
        {
            if (string.Equals(
                    chosenOption,
                    OptionCreateSmsAnyway,
                    StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    chosenOption,
                    OptionCreateEmailAnyway,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return AssistantEmptyChannelAudienceChoice.CreateAnyway;
            }

            if (string.Equals(
                    chosenOption,
                    OptionWaitSms,
                    StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    chosenOption,
                    OptionWaitEmail,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return AssistantEmptyChannelAudienceChoice.Wait;
            }

            if (string.Equals(
                    chosenOption,
                    OptionBroaden,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                // Single-channel Campaigns: broaden means switch to the other
                // channel that still has eligible guests when present.
                return AssistantEmptyChannelAudienceChoice.BroadenToEmail;
            }

            if (string.Equals(
                    emptyChannelId,
                    "sms",
                    StringComparison.OrdinalIgnoreCase
                )
                && string.Equals(
                    chosenOption,
                    OptionUseEmail,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return AssistantEmptyChannelAudienceChoice.SwitchChannel;
            }

            if (string.Equals(
                    emptyChannelId,
                    "email",
                    StringComparison.OrdinalIgnoreCase
                )
                && string.Equals(
                    chosenOption,
                    OptionUseSms,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return AssistantEmptyChannelAudienceChoice.SwitchChannel;
            }

            return null;
        }

        public static string AlternateChannelLabel(string emptyChannelId)
            => string.Equals(emptyChannelId, "sms", StringComparison.OrdinalIgnoreCase)
                ? "Email"
                : "SMS";

        public static string EmptyChannelLabel(string emptyChannelId)
            => string.Equals(emptyChannelId, "sms", StringComparison.OrdinalIgnoreCase)
                ? "SMS"
                : "Email";
    }
}
