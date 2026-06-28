namespace TummlyBackend.DTOs.Admin
{
    public class AdminOperatorLocationDto
    {
        public string LocationName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? Postcode { get; set; }

        public string? LocationPhone { get; set; }

        public string? LocalContact { get; set; }
    }
}
