namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Recovery offer chrome for Guest response email (title, description,
    /// short text code, expiry). QR is deferred until minting exists.
    /// </summary>
    public sealed record GuestResponseEmailOfferBlock(
        string Title,
        string Description,
        string RedemptionCode,
        string ExpiryLabel
    );
}
