using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Infrastructure;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Infrastructure
{
    public class ObjectStorageRegistrationTests
    {
        [Fact]
        public void AddQueryAttachmentStorage_defaults_to_S3()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            services.AddLogging();
            services.AddQueryAttachmentStorage(configuration);

            using var provider = services.BuildServiceProvider();
            var storage = provider.GetRequiredService<IQueryAttachmentStorage>();

            Assert.IsType<S3QueryAttachmentStorage>(storage);
        }

        [Fact]
        public void AddQueryAttachmentStorage_selects_AzureBlob_when_Provider_set()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ObjectStorage:Provider"] = "AzureBlob",
                        ["ObjectStorage:Endpoint"] =
                            "https://stexample.blob.core.windows.net",
                        ["ObjectStorage:Bucket"] = "help-centre-attachments",
                    }
                )
                .Build();

            services.AddLogging();
            services.AddQueryAttachmentStorage(configuration);

            using var provider = services.BuildServiceProvider();
            var storage = provider.GetRequiredService<IQueryAttachmentStorage>();

            Assert.IsType<AzureBlobQueryAttachmentStorage>(storage);
        }
    }
}
