namespace TummlyBackend.DTOs.AccountWorkspace
{
    public sealed class GuestDataExportResult
    {
        public required string FileName { get; init; }

        public required string ContentType { get; init; }

        public required byte[] Content { get; init; }
    }
}
