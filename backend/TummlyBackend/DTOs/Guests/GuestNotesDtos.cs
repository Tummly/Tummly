namespace TummlyBackend.DTOs.Guests
{
    public sealed class CreateGuestNoteRequest
    {
        public string Body { get; set; }
            = string.Empty;
    }

    public sealed class GuestNoteItemDto
    {
        public int Id { get; init; }

        public string Body { get; init; }
            = string.Empty;

        public string AuthorDisplayName { get; init; }
            = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    public sealed class GuestNotesListResponse
    {
        public IReadOnlyList<GuestNoteItemDto> Items { get; init; }
            = Array.Empty<GuestNoteItemDto>();

        public int TotalCount { get; init; }
    }
}
