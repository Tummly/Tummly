namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Downloadable file attachment (e.g. VAT invoice PDF). Distinct from
    /// <see cref="EmailInlineImage"/> CID chrome images.
    /// </summary>
    public sealed record EmailFileAttachment(
        string Filename,
        byte[] Content,
        string ContentType
    );
}
