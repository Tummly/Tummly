namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Guest-facing offer chrome for Guest response / Campaign email
    /// (Offer claim QR from RedemptionCode, title, description, code, expiry).
    /// </summary>
    public sealed record GuestResponseEmailOfferBlock(
        string Title,
        string Description,
        string RedemptionCode,
        string ExpiryLabel
    );
}
