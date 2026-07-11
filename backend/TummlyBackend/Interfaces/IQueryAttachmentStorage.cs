namespace TummlyBackend.Interfaces
{
    public interface IQueryAttachmentStorage
    {
        bool IsConfigured { get; }

        Task UploadAsync(
            string storageKey,
            Stream content,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default
        );

        Task<Stream> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        );

        Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        );
    }
}
