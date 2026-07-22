namespace TummlyBackend.Models
{
    /// <summary>
    /// Membership of a Guest tag catalog entry on a Location Guest.
    /// </summary>
    public class LocationGuestTag
    {
        public int LocationGuestId { get; set; }

        public LocationGuest? LocationGuest { get; set; }

        public int GuestTagId { get; set; }

        public GuestTag? GuestTag { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
