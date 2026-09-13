namespace TummlyBackend.PrintReadyQrMaterials
{
    public sealed class PrintTemplatePackSnapshot
    {
        public required string Id { get; init; }

        public required string DefaultOfferHeadline { get; init; }

        public required string OfferCopyVersion { get; init; }

        public required string TentStickerEmptySvgPath { get; init; }

        public required string CardSvgPath { get; init; }

        public required PrintSlotQrSpec TableTentQr { get; init; }

        public required PrintSlotQrSpec WindowStickerQr { get; init; }

        public required PrintSlotRectSpec OfferCardHeadline { get; init; }

        public required PrintSlotQrSpec OfferCardQr { get; init; }
    }

    public sealed class PrintSlotQrSpec
    {
        public double? XMm { get; init; }

        public double? YMm { get; init; }

        public required double WidthMm { get; init; }

        public required double HeightMm { get; init; }

        public bool CentreWhenBlank { get; init; }
    }

    public sealed class PrintSlotRectSpec
    {
        public required double XMm { get; init; }

        public required double YMm { get; init; }

        public required double WidthMm { get; init; }

        public required double HeightMm { get; init; }
    }
}
