namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Inline CID image for guest-facing mail — Gmail strips data URIs.
    /// </summary>
    public sealed record EmailInlineImage(
        string ContentId,
        string Filename,
        byte[] Content
    );
}
