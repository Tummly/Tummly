using System.Collections.Concurrent;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// In-memory object storage for tests and local generation without cloud.
    /// </summary>
    public sealed class InMemoryQueryAttachmentStorage : IQueryAttachmentStorage
    {
        private readonly ConcurrentDictionary<
            string,
            (byte[] Bytes, string ContentType)
        > _objects = new(StringComparer.Ordinal);

        public bool IsConfigured => true;

        public Task UploadAsync(
            string storageKey,
            Stream content,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default
        )
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            _objects[storageKey] = (ms.ToArray(), contentType);
            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            if (!_objects.TryGetValue(storageKey, out var stored))
            {
                throw new FileNotFoundException(storageKey);
            }

            return Task.FromResult<Stream>(new MemoryStream(stored.Bytes));
        }

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            _objects.TryRemove(storageKey, out _);
            return Task.CompletedTask;
        }
    }
}
