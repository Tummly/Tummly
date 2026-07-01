namespace TummlyBackend.DTOs.Admin
{
    public class AdminTrialRequestDto
    {
        public int Id { get; set; }

        public string BusinessName { get; set; } = string.Empty;

        public string BusinessCategory { get; set; } = string.Empty;

        public string Locations { get; set; } = string.Empty;

        public string? BusinessLink { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Mobile { get; set; } = string.Empty;

        public string MainLocation { get; set; } = string.Empty;

        public string TownCity { get; set; } = string.Empty;

        public string MainLocationPostcode { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Goal { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; }

        public bool IsApproved { get; set; }

        public bool IsAccountCreated { get; set; }

        public string AccountType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? ReviewedBy { get; set; }

        public DateTime? DeclinedAt { get; set; }

        public string? DeclineReason { get; set; }

        public DateTime? MoreInfoRequestedAt { get; set; }

        public string? MoreInfoMessage { get; set; }

        public DateTime? InviteSentAt { get; set; }

        public DateTime? InviteExpiresAt { get; set; }

        public DateTime? AccountCreatedAt { get; set; }

        public string? PrimaryAddress { get; set; }

        public string? PrimaryPostcode { get; set; }

        public List<AdminOperatorLocationDto> OperatorLocations { get; set; } =
            new();

        public int? OperatorUserId { get; set; }

        public string? ActivationStatus { get; set; }

        public string? ActivationStatusDetail { get; set; }

        public DateTime? ActivationExpiresAt { get; set; }

        public string? ActivationCode { get; set; }
    }
}
