using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Infrastructure
{
    public static class ObjectStorageRegistration
    {
        public static IServiceCollection AddQueryAttachmentStorage(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.Configure<ObjectStorageSettings>(
                configuration.GetSection("ObjectStorage")
            );

            var provider =
                configuration["ObjectStorage:Provider"]
                ?? ObjectStorageSettings.S3Provider;

            if (
                string.Equals(
                    provider,
                    ObjectStorageSettings.AzureBlobProvider,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                services.AddSingleton<
                    IBlobObjectClient,
                    AzureSdkBlobObjectClient
                >();
                services.AddSingleton<
                    IQueryAttachmentStorage,
                    AzureBlobQueryAttachmentStorage
                >();
            }
            else
            {
                services.AddSingleton<
                    IQueryAttachmentStorage,
                    S3QueryAttachmentStorage
                >();
            }

            return services;
        }
    }
}
