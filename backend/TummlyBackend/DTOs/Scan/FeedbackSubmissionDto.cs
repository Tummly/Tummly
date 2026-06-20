using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.DTOs.Scan
{
    public class FeedbackSubmissionDto
    {
        [MaxLength(150)]
        public string? GuestName { get; set; }

        [MaxLength(100)]
        public string? GuestContact { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
