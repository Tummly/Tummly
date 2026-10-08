namespace TummlyBackend.DTOs.Reports
{
    public sealed class ReportsCaptureFunnelDto
    {
        public ReportsMetricDto QrScans { get; init; } = new();

        public ReportsMetricDto FeedbackSubmitted { get; init; } = new();

        public ReportsMetricDto ContactableGuests { get; init; } = new();

        public ReportsMetricDto OfferClaimed { get; init; } = new();
    }

    public sealed class ReportsCapturePlacementDto
    {
        public int QrCodeId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public int Scans { get; init; }

        /// <summary>Blank until a Form-open event exists (REP-02 / REP-03).</summary>
        public int? FormOpens { get; init; }

        public int Feedback { get; init; }

        public int Contactable { get; init; }

        /// <summary>Guest-form thank-you claims attributed to this QR.</summary>
        public int Claims { get; init; }

        /// <summary>Feedback / scans when scans &gt; 0; otherwise null.</summary>
        public double? ConversionPercent { get; init; }
    }

    public sealed class ReportsCaptureDto
    {
        public bool LifetimeEmpty { get; init; }

        public ReportsCaptureFunnelDto? Funnel { get; init; }

        public IReadOnlyList<ReportsCapturePlacementDto>? Placements { get; init; }
    }
}
