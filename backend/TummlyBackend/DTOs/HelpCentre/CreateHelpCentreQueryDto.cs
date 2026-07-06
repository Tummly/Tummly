namespace TummlyBackend.DTOs.HelpCentre
{
    public class CreateHelpCentreQueryDto
    {
        public string Topic { get; set; } = string.Empty;

        public string BusinessName { get; set; } = string.Empty;

        public string SubmitterName { get; set; } = string.Empty;

        public string SubmitterEmail { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public int? RestaurantLocationId { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
