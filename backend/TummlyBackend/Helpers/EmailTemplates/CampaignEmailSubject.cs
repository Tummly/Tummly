namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Campaign / Offer unlocked email subject —
    /// Figma Guest-Loop-MVP 6852:49454.
    /// </summary>
    public static class CampaignEmailSubject
    {
        public static string Format(string? offerTitle, string? restaurantName)
        {
            var offer = string.IsNullOrWhiteSpace(offerTitle)
                ? "offer"
                : offerTitle.Trim();
            var restaurant = string.IsNullOrWhiteSpace(restaurantName)
                ? "us"
                : restaurantName.Trim();
            return $"Your {offer} from {restaurant}";
        }
    }
}
