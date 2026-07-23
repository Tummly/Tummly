using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.DTOs.Guests
{
    public class CreateGuestTagDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
            = string.Empty;
    }

    public class ApplyGuestTagsDto
    {
        [Required]
        [MinLength(1)]
        public List<int> GuestIds { get; set; }
            = new();

        [Required]
        [MinLength(1)]
        public List<int> TagIds { get; set; }
            = new();
    }

    /// <summary>
    /// Desired membership set for each guest (adds missing, removes extras).
    /// Empty <see cref="TagIds"/> clears all memberships.
    /// </summary>
    public class SyncGuestTagsDto
    {
        [Required]
        [MinLength(1)]
        public List<int> GuestIds { get; set; }
            = new();

        [Required]
        public List<int> TagIds { get; set; }
            = new();
    }
}
