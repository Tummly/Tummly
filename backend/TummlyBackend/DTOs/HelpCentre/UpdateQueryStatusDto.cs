namespace TummlyBackend.DTOs.HelpCentre
{
    public class UpdateQueryStatusDto
    {
        public string Status { get; set; } = string.Empty;

        public string? EscalationNote { get; set; }
    }
}
