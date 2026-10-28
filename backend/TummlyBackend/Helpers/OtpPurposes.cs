namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Purpose labels stored on <c>OtpVerification.Purpose</c>. Codes for different
    /// purposes never verify against each other.
    /// </summary>
    public static class OtpPurposes
    {
        public const string Signup = "signup";

        public const string SignIn = "sign_in";

        public const string Trial = "trial";

        public const string TeamInvite = "team_invite";

        public const string GuestPreferences = "guest_preferences";
    }
}
