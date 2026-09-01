namespace TummlyBackend.DTOs.Locations
{
    public sealed class LocationDetailHeaderDto
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? City { get; init; }

        public string LifecycleStatus { get; init; } = string.Empty;

        public string SetupStatus { get; init; } = string.Empty;

        public string? ManagerName { get; init; }

        public int? ManagerUserId { get; init; }

        public string Address { get; init; } = string.Empty;

        public string? Postcode { get; init; }

        public string? LocationPhone { get; init; }

        public string? LocalContact { get; init; }

        public int LiveQrCount { get; init; }

        public int GuestsCapturedThisMonth { get; init; }
    }

    public sealed class LocationDetailTeamAccessRowDto
    {
        public int MembershipId { get; init; }

        public int UserId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Role { get; init; } = string.Empty;

        public string AccessLabel { get; init; } = string.Empty;

        public DateTime? LastActiveAt { get; init; }
    }

    public sealed class LocationDetailResponseDto
    {
        public bool Success { get; init; } = true;

        public LocationDetailHeaderDto Header { get; init; } = new();

        /// <summary>
        /// Checklist item id → status id (complete | optional | incomplete | not-started).
        /// </summary>
        public Dictionary<string, string> SetupChecklist { get; init; } = new();

        public List<LocationDetailTeamAccessRowDto> TeamAccessRows { get; init; } = [];
    }
}
