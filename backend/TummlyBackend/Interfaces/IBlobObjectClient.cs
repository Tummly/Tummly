namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Thin Azure Blob container port used by
    /// <see cref="Services.AzureBlobQueryAttachmentStorage"/>.
    /// </summary>
    public interface IBlobObjectClient
    {
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
