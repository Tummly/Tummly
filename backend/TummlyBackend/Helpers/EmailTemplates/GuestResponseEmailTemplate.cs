namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Venue-branded Guest response / Campaign email HTML — shared send path.
    /// Chrome lives in <see cref="BaseNonTransactionalEmailTemplate"/>.
    /// </summary>
    public static class GuestResponseEmailTemplate
    {
        public static string Generate(
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string? subject,
            string message,
            string frontendBaseUrl,
            string? brandLogoUrl,
            GuestResponseEmailOfferBlock? offer = null
        )
        {
            return BaseNonTransactionalEmailTemplate.Generate(
                brandTitle,
                brandSubtitle,
                locationAddress,
                subject,
                message,
                frontendBaseUrl,
                brandLogoUrl,
                offer
            );
        }
    }
}
