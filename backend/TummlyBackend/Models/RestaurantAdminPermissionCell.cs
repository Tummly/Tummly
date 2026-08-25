using System.ComponentModel.DataAnnotations;
using TummlyBackend.Helpers;

namespace TummlyBackend.Models
{
    public class RestaurantAdminPermissionCell
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        [Required]
        [MaxLength(40)]
        public string AreaId { get; set; } = string.Empty;

        public PermissionLevel Level { get; set; }
    }
}
