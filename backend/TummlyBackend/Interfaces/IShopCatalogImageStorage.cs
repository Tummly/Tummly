namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Object storage for Admin-uploaded Tummly Shop catalog images
    /// (dedicated container / bucket).
    /// </summary>
    public interface IShopCatalogImageStorage
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
