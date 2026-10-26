namespace TummlyBackend.Configurations
{
    public class TwilioSettings
    {
        /// <summary>
        /// Twilio account SID (starts with AC).
        /// </summary>
        public string AccountSid { get; set; } = string.Empty;

        /// <summary>
        /// Twilio auth token.
        /// </summary>
        public string AuthToken { get; set; } = string.Empty;

        /// <summary>
        /// Verify service SID (starts with VA).
        /// </summary>
        public string VerifyServiceSid { get; set; } = string.Empty;

        /// <summary>
        /// Outbound Recovery SMS sender (E.164 or Twilio sender id).
        /// </summary>
        public string RecoveryFromNumber { get; set; } = string.Empty;

        /// <summary>
        /// ISO 3166-1 alpha-2 region used when parsing numbers without a + prefix.
        /// Defaults to GB (United Kingdom).
        /// </summary>
        public string DefaultRegion { get; set; } = "GB";

        /// <summary>
        /// Legacy alias for <see cref="DefaultRegion"/> when set as a calling code (e.g. 44).
        /// </summary>
        public string DefaultCountryCode { get; set; } = "44";

        public string ResolvedDefaultRegion =>
            !string.IsNullOrWhiteSpace(DefaultRegion) &&
            DefaultRegion.Trim().Length == 2 &&
            char.IsLetter(DefaultRegion.Trim()[0])
                ? DefaultRegion.Trim().ToUpperInvariant()
                : DefaultCountryCode.Trim().TrimStart('+') switch
                {
                    "44" => "GB",
                    _ => "GB",
                };

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AccountSid) &&
            !string.IsNullOrWhiteSpace(AuthToken) &&
            !string.IsNullOrWhiteSpace(VerifyServiceSid);

        public bool IsRecoverySmsConfigured =>
            !string.IsNullOrWhiteSpace(AccountSid) &&
            !string.IsNullOrWhiteSpace(AuthToken) &&
            !string.IsNullOrWhiteSpace(RecoveryFromNumber);
    }
}
