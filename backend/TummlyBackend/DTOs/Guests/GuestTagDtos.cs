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
}
