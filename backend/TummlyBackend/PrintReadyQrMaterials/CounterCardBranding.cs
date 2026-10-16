namespace TummlyBackend.PrintReadyQrMaterials
{
    /// <summary>
    /// Per-Location fields painted onto the A5 Counter Display PDF.
    /// </summary>
    public sealed record CounterCardBranding(
        string LocationName,
        string AddressLine,
        byte[]? LogoBytes,
        string? LogoContentType
    )
    {
        public static CounterCardBranding Empty { get; } =
            new(string.Empty, string.Empty, null, null);
    }

    /// <summary>
    /// SVG parts from the Design Counter Card drop (print-template-pack).
    /// </summary>
    public sealed record CounterCardAssetPaths(
        string? PatternSvgPath,
        string? StripSvgPath,
        string? PoweredSvgPath
    );
}
