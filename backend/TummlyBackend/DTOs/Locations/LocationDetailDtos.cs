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

    public sealed class LocationDetailOverviewMetricsDto
    {
        public int QrScans { get; init; }

        public int FormStarts { get; init; }

        public int Feedback { get; init; }

        public int GuestsCaptured { get; init; }

        public int OptIns { get; init; }

        public int OffersClaimed { get; init; }

        public int OffersRedeemed { get; init; }
    }

    public sealed class LocationDetailQrRowDto
    {
        public int QrCodeId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Placement { get; init; } = string.Empty;

        public string StatusLabel { get; init; } = string.Empty;

        public int Scans { get; init; }

        public int Starts { get; init; }

        public int Submissions { get; init; }

        public int OptIns { get; init; }

        public int Claims { get; init; }

        public DateTime? LastScanAtUtc { get; init; }
    }

    public sealed class LocationDetailOfferCardDto
    {
        public int EntityId { get; init; }

        public string Kind { get; init; } = string.Empty;

        public string StatusLabel { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Meta { get; init; } = string.Empty;

        public string PrimaryCta { get; init; } = string.Empty;

        public string SecondaryCta { get; init; } = string.Empty;
    }

    public sealed class LocationDetailResponseDto
    {
        public bool Success { get; init; } = true;

        public LocationDetailHeaderDto Header { get; init; } = new();

        /// <summary>
        /// Checklist item id → status id (complete | optional | incomplete | not-started).
        /// </summary>
        public Dictionary<string, string> SetupChecklist { get; init; } = new();

        public LocationDetailOverviewMetricsDto OverviewMetrics { get; init; } = new();

        public IReadOnlyList<LocationDetailQrRowDto> QrRows { get; init; }
            = Array.Empty<LocationDetailQrRowDto>();

        public IReadOnlyList<LocationDetailOfferCardDto> OfferCards { get; init; }
            = Array.Empty<LocationDetailOfferCardDto>();
    }
}
