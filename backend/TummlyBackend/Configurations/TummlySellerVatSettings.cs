namespace TummlyBackend.Configurations
{
    /// <summary>
    /// Seller VAT / legal identity for live paid conversion (pack
    /// <c>TUMMLY_VAT_*</c> / legal env keys).
    /// </summary>
    public class TummlySellerVatSettings
    {
        public const string RegistrationNumberKey =
            "TUMMLY_VAT_REGISTRATION_NUMBER";

        public const string EffectiveDateKey = "TUMMLY_VAT_EFFECTIVE_DATE";

        public const string LegalNameKey = "TUMMLY_LEGAL_NAME";

        public const string RegisteredAddressKey = "TUMMLY_REGISTERED_ADDRESS";

        public string RegistrationNumber { get; set; } = string.Empty;

        public string EffectiveDate { get; set; } = string.Empty;

        public string LegalName { get; set; } = string.Empty;

        public string RegisteredAddress { get; set; } = string.Empty;

        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(RegistrationNumber)
            && !string.IsNullOrWhiteSpace(EffectiveDate)
            && !string.IsNullOrWhiteSpace(LegalName)
            && !string.IsNullOrWhiteSpace(RegisteredAddress);
    }
}
