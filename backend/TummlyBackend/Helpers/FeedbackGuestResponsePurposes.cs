namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server allow-list for Respond purposes. Matches the service purposes the
    /// Operator Respond wizard offers. A promotional purpose (for example the
    /// Recovery Offer purpose) or any unknown value is refused. Offer sends
    /// use their own endpoint and purpose.
    /// </summary>
    public static class FeedbackGuestResponsePurposes
    {
        public const string ErrorCode = "guest_response_purpose_not_allowed";

        private static readonly string[] AllowedIds =
        {
            "acknowledge_feedback",
            "apologise_and_confirm_follow_up",
            "ask_for_more_information",
            "confirm_operational_action",
            "create_custom_response",
        };

        public static string Message { get; } =
            "Purpose must be one of: "
            + string.Join(", ", AllowedIds)
            + ".";

        /// <summary>
        /// A blank purpose is allowed (none recorded, as before). A non-blank
        /// purpose must match an allowed id exactly after trimming.
        /// </summary>
        public static bool IsAllowed(string? purpose)
        {
            if (string.IsNullOrWhiteSpace(purpose))
            {
                return true;
            }

            return AllowedIds.Contains(purpose.Trim(), StringComparer.Ordinal);
        }
    }
}
