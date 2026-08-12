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
            string tummlyLogoDataUri,
            string? brandLogoUrl,
            GuestResponseEmailOfferBlock? offer = null,
            string? topDecorationDataUri = null,
            string? bottomStripDataUri = null
        )
        {
            return BaseNonTransactionalEmailTemplate.Generate(
                brandTitle,
                brandSubtitle,
                locationAddress,
                subject,
                message,
                frontendBaseUrl,
                tummlyLogoDataUri,
                brandLogoUrl,
                offer,
                topDecorationDataUri,
                bottomStripDataUri
            );
        }
    }
}
