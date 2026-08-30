namespace TummlyBackend.DTOs.SmartGuestLink
{
    /// <summary>
    /// Guest QR resolve after Tick: Live metadata, branded Dormant, or not-found
    /// (invalid / inactive / Pause).
    /// </summary>
    public abstract record GuestQrResolveResult
    {
        public sealed record NotFound : GuestQrResolveResult;

        public sealed record Live(GuestLinkLocationInfo Location) : GuestQrResolveResult;

        public sealed record Dormant(
            string RestaurantName,
            string? BrandLogoPublicUrl
        ) : GuestQrResolveResult;
    }
}
