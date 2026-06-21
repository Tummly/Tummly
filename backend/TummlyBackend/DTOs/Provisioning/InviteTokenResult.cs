namespace TummlyBackend.DTOs.Provisioning
{
    public class InviteTokenResult
    {
        public string AccountType { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string RestaurantName { get; set; } = string.Empty;

        public string GroupName { get; set; } = string.Empty;

        public string Mobile { get; set; } = string.Empty;

        public string BusinessCategory { get; set; } = string.Empty;

        public string? Locations { get; set; }

        public string Role { get; set; } = string.Empty;

        public int TrialRequestId { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
