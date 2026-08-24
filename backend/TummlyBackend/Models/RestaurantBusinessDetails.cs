using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Optional legal profile for a Restaurant (1:1). Empty profile is valid.
    /// </summary>
    public class RestaurantBusinessDetails
    {
        [Key]
        [ForeignKey(nameof(Restaurant))]
        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        /// <summary>
        /// Closed set: sole-trader, partnership, limited-company, llp, plc, other.
        /// </summary>
        [MaxLength(32)]
        public string? LegalStructure { get; set; }

        [MaxLength(200)]
        public string? LegalBusinessName { get; set; }

        [MaxLength(200)]
        public string? TradingName { get; set; }

        [MaxLength(50)]
        public string? CompanyNumber { get; set; }

        [MaxLength(50)]
        public string? VatNumber { get; set; }

        [MaxLength(100)]
        public string? CountryOfRegistration { get; set; }

        [MaxLength(500)]
        public string? AddressLine1 { get; set; }

        [MaxLength(500)]
        public string? AddressLine2 { get; set; }

        [MaxLength(150)]
        public string? TownCity { get; set; }

        [MaxLength(150)]
        public string? County { get; set; }

        [MaxLength(20)]
        public string? Postcode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }
    }
}
