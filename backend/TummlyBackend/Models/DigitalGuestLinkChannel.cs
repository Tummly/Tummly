namespace TummlyBackend.Models
{
    /// <summary>
    /// Where a Digital guest link will be used (Create digital guest link
    /// "Where will you use it?"). Wire uses enum names (e.g. SocialMedia).
    /// </summary>
    public enum DigitalGuestLinkChannel
    {
        SocialMedia = 0,

        Email = 1,

        WhatsApp = 2,

        Website = 3,

        OnlineOrdering = 4,

        Other = 5,
    }
}
