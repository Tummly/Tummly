namespace TummlyBackend.DTOs.SmartGuestLink
{
    public class GuestLinkLocationInfo
    {
        public int LocationId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        public string LocationName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}
