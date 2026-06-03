namespace TummlyBackend.DTOs.Admin
{
    public class UpdateTrialStatusDto
    {
        public int TrialRequestId { get; set; }

        public string Status { get; set; }
            = string.Empty;

        public string? AdminNotes { get; set; }

        public string? MoreInfoMessage { get; set; }

        public string? DeclineReason { get; set; }
    }
}