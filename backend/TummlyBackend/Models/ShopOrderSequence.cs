namespace TummlyBackend.Models
{
    /// <summary>
    /// Per-restaurant counter for public Shop order numbers (ORD-{n}).
    /// </summary>
    public class ShopOrderSequence
    {
        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        /// <summary>Next number to allocate (starts at 1).</summary>
        public int NextNumber { get; set; } = 1;
    }
}
