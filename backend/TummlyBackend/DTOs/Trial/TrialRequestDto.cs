namespace TummlyBackend.DTOs.Trial
{
    public class TrialRequestDto
    {
        public string BusinessName { get; set; }

        public string BusinessCategory { get; set; }

        public string Locations { get; set; }

        public string? BusinessLink { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string Mobile { get; set; }

        public string Role { get; set; }

        public string Goal { get; set; }

        public bool TermsAccepted { get; set; }


    }
}