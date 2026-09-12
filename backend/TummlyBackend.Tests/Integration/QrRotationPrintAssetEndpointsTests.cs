using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public sealed class QrRotationPrintAssetEndpointsTests
    {
        private static readonly byte[] OldAssetBytes =
            "%PDF-old-token-asset"u8.ToArray();

        [Fact]
        public async Task Rotate_ReturnsSuccessWhileFailingPrintWorkIsBlocked()
        {
            var printWork = new BlockingFailingPrintWork();
            await using var rootFactory = new TummlyWebApplicationFactory();
            await using var factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IPrintReadyQrMaterialsWork>();
                    services.AddSingleton<IPrintReadyQrMaterialsWork>(printWork);
                });
            });
            using var client = factory.CreateClient();
            var seeded = await SeedReadyAssetAsync(factory.Services);

            using var request = AuthorizedRequest(
                HttpMethod.Post,
                $"/api/capture/placements/{seeded.QrCodeId}/rotate?locationId={seeded.LocationId}",
                seeded.OwnerJwt
            );

            try
            {
                using var response = await client
                    .SendAsync(request)
                    .WaitAsync(TimeSpan.FromSeconds(5));

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(
                    seeded.LocationId,
                    await printWork.Requested.WaitAsync(
                        TimeSpan.FromSeconds(1)
                    )
                );
                Assert.False(printWork.ProcessingCompleted.IsCompleted);

                using var scope = factory.Services.CreateScope();
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var qrCode = await context.QrCodes
                    .AsNoTracking()
                    .SingleAsync(row => row.Id == seeded.QrCodeId);
                Assert.NotEqual(seeded.OldToken, qrCode.Token);

                var asset = await LoadAssetAsync(
                    context,
                    seeded.LocationId
                );
                Assert.Equal(
                    PrintReadyQrAssetStatus.Preparing,
                    asset.Status
                );
                Assert.Null(asset.StorageKey);
                Assert.Null(asset.QrTokenFingerprint);
            }
            finally
            {
                printWork.Release();
                await printWork.ProcessingCompleted.WaitAsync(
                    TimeSpan.FromSeconds(1)
                );
            }

            Assert.True(printWork.GenerationFailed);
        }

        [Fact]
        public async Task AdminDownload_AfterRotate_ServesRegeneratedNewTokenAsset()
        {
            var storage = new TrackingAttachmentStorage();
            await using var rootFactory = new TummlyWebApplicationFactory();
            await using var factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IQueryAttachmentStorage>();
                    services.AddSingleton<IQueryAttachmentStorage>(storage);
                });
            });
            using var client = factory.CreateClient();
            var seeded = await SeedReadyAssetAsync(
                factory.Services,
                storage
            );

            using var rotate = AuthorizedRequest(
                HttpMethod.Post,
                $"/api/capture/placements/{seeded.QrCodeId}/rotate?locationId={seeded.LocationId}",
                seeded.OwnerJwt
            );
            var rotateResponse = await client.SendAsync(rotate);
            Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);

            string newToken;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                newToken = await context.QrCodes
                    .Where(row => row.Id == seeded.QrCodeId)
                    .Select(row => row.Token)
                    .SingleAsync();
                Assert.NotEqual(seeded.OldToken, newToken);

                var invalidated = await LoadAssetAsync(
                    context,
                    seeded.LocationId
                );
                Assert.Equal(
                    PrintReadyQrAssetStatus.Preparing,
                    invalidated.Status
                );
            }

            var printWork = factory.Services
                .GetRequiredService<IPrintReadyQrMaterialsWork>();
            await printWork.DrainAsync();

            string regeneratedStorageKey;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var regenerated = await LoadAssetAsync(
                    context,
                    seeded.LocationId
                );
                regeneratedStorageKey = Assert.IsType<string>(
                    regenerated.StorageKey
                );
                Assert.Equal(
                    PrintReadyQrAssetStatus.Ready,
                    regenerated.Status
                );
                Assert.Equal(
                    Fingerprint(newToken),
                    regenerated.QrTokenFingerprint
                );
                Assert.Contains(
                    Fingerprint(newToken),
                    regeneratedStorageKey
                );
                Assert.NotEqual(
                    seeded.OldStorageKey,
                    regeneratedStorageKey
                );
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        regenerated.TemplatePackVersion
                    )
                );
                Assert.NotEqual(
                    "old-pack",
                    regenerated.TemplatePackVersion
                );

                var regeneratedShopAsset = await LoadAssetAsync(
                    context,
                    seeded.LocationId,
                    seeded.ShopOrderId
                );
                Assert.Equal(
                    PrintReadyQrAssetStatus.Ready,
                    regeneratedShopAsset.Status
                );
                Assert.Equal(
                    Fingerprint(newToken),
                    regeneratedShopAsset.QrTokenFingerprint
                );
                Assert.Equal(
                    regeneratedStorageKey,
                    regeneratedShopAsset.StorageKey
                );
            }

            using var download = AuthorizedRequest(
                HttpMethod.Get,
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/TableTent/download",
                seeded.AdminJwt
            );
            var downloadResponse = await client.SendAsync(download);

            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
            Assert.Equal(
                "application/pdf",
                downloadResponse.Content.Headers.ContentType?.MediaType
            );
            var downloadedBytes =
                await downloadResponse.Content.ReadAsByteArrayAsync();
            Assert.Equal(
                storage.GetRequired(regeneratedStorageKey),
                downloadedBytes
            );
            Assert.False(downloadedBytes.SequenceEqual(OldAssetBytes));
            Assert.Equal(
                OldAssetBytes,
                storage.GetRequired(seeded.OldStorageKey)
            );

            using var shopDownload = AuthorizedRequest(
                HttpMethod.Get,
                $"/api/admin/shop-orders/{seeded.ShopOrderId}/print-assets/TableTent/download",
                seeded.AdminJwt
            );
            var shopDownloadResponse = await client.SendAsync(shopDownload);
            Assert.Equal(HttpStatusCode.OK, shopDownloadResponse.StatusCode);
            Assert.Equal(
                downloadedBytes,
                await shopDownloadResponse.Content.ReadAsByteArrayAsync()
            );
        }

        private static async Task<SeededAsset> SeedReadyAssetAsync(
            IServiceProvider services,
            TrackingAttachmentStorage? storage = null
        )
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Rotate Print Owner",
                Email = $"rotate-print-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900888",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Rotate Print Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
                BillingAccount = BillingCreditsService
                    .CreateDefaultBillingAccount(
                        0,
                        "TUMMLY-UK-GBP-2026-08-V3"
                    ),
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;
            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                City = "Leeds",
                Postcode = "LS1 1AA",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var oldToken = $"rotate-print-old-{Guid.NewGuid():N}"[..32];
            var qrCode = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.TableTent,
                Token = oldToken,
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qrCode);

            var shopOrder = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{Guid.NewGuid():N}"[..20],
                RestaurantId = restaurant.Id,
                LocationId = location.Id,
                LocationNameSnapshot = location.LocationName,
                PlacedByUserId = owner.Id,
                PlacedByNameSnapshot = owner.FullName,
                MaterialsNetPence = 2400,
                VatPence = 480,
                GrossPence = 2880,
                DeliveryMethod = ShopDeliveryMethods.Standard,
                PaymentStatus = ShopPaymentStatuses.Paid,
                FulfilmentStatus = ShopFulfilmentStatuses.Processing,
                PaidAtUtc = DateTime.UtcNow,
                ProcessingStartedAtUtc = DateTime.UtcNow,
                ShipToContactName = owner.FullName,
                ShipToAddressLine1 = location.Address,
                ShipToPostcode = location.Postcode,
                ShipToCountry = "United Kingdom",
                Lines =
                {
                    new ShopOrderLine
                    {
                        Id = Guid.NewGuid(),
                        CatalogSkuId = "table-tents",
                        TitleSnapshot = "Table tents",
                        MaterialType = "tabletop",
                        Quantity = 1,
                        UnitNetPence = 2400,
                        LineNetPence = 2400,
                    },
                },
            };
            context.ShopOrders.Add(shopOrder);

            var oldStorageKey =
                $"print-ready-qr/{location.Id}/TableTent/{Fingerprint(oldToken)}.pdf";
            context.PrintReadyQrAssets.AddRange(
                ReadyAsset(shopOrderId: null),
                ReadyAsset(shopOrder.Id)
            );

            var admin = new Admin
            {
                FullName = "Rotate Print Admin",
                Email = $"rotate-print-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();

            storage?.Seed(oldStorageKey, OldAssetBytes);

            return new SeededAsset(
                owner.Id,
                location.Id,
                qrCode.Id,
                shopOrder.Id,
                oldToken,
                oldStorageKey,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                jwtService.GenerateAdminToken(admin)
            );

            PrintReadyQrAsset ReadyAsset(Guid? shopOrderId) =>
                new()
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.TableTent,
                    ShopOrderId = shopOrderId,
                    Status = PrintReadyQrAssetStatus.Ready,
                    StorageKey = oldStorageKey,
                    ContentType = "application/pdf",
                    FileName = "old-table-tent.pdf",
                    QrTokenFingerprint = Fingerprint(oldToken),
                    TemplatePackVersion = "old-pack",
                    OfferCopyVersion = "old-copy",
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddHours(-1),
                };
        }

        private static Task<PrintReadyQrAsset> LoadAssetAsync(
            ApplicationDbContext context,
            int locationId,
            Guid? shopOrderId = null
        ) =>
            context.PrintReadyQrAssets
                .AsNoTracking()
                .SingleAsync(row =>
                    row.RestaurantLocationId == locationId
                    && row.QrType == QrType.TableTent
                    && row.ShopOrderId == shopOrderId
                );

        private static HttpRequestMessage AuthorizedRequest(
            HttpMethod method,
            string url,
            string jwt
        )
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static string Fingerprint(string token)
        {
            var hash = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(token)
            );
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private sealed record SeededAsset(
            int OwnerUserId,
            int LocationId,
            int QrCodeId,
            Guid ShopOrderId,
            string OldToken,
            string OldStorageKey,
            string OwnerJwt,
            string AdminJwt
        );

        private sealed class BlockingFailingPrintWork
            : IPrintReadyQrMaterialsWork
        {
            private readonly TaskCompletionSource<int> _requested = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            private readonly TaskCompletionSource<bool> _release = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            private readonly TaskCompletionSource<bool> _completed = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            public Task<int> Requested => _requested.Task;

            public Task ProcessingCompleted => _completed.Task;

            public bool GenerationFailed { get; private set; }

            public ValueTask RequestEnsureAsync(
                int locationId,
                CancellationToken cancellationToken = default
            )
            {
                _requested.TrySetResult(locationId);
                _ = ProcessAsync();
                return ValueTask.CompletedTask;
            }

            public ValueTask RequestShopOrderEnsureAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public Task RunAsync(CancellationToken stoppingToken) =>
                Task.CompletedTask;

            public Task DrainAsync(
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public void Release() => _release.TrySetResult(true);

            private async Task ProcessAsync()
            {
                try
                {
                    await _release.Task;
                    throw new IOException(
                        "Controlled print generation failure."
                    );
                }
                catch (IOException)
                {
                    GenerationFailed = true;
                }
                finally
                {
                    _completed.TrySetResult(true);
                }
            }
        }

        private sealed class TrackingAttachmentStorage
            : IQueryAttachmentStorage
        {
            private readonly ConcurrentDictionary<string, byte[]> _files =
                new();

            public bool IsConfigured => true;

            public void Seed(string storageKey, byte[] content) =>
                _files[storageKey] = content.ToArray();

            public byte[] GetRequired(string storageKey) =>
                _files.TryGetValue(storageKey, out var content)
                    ? content
                    : throw new FileNotFoundException(storageKey);

            public async Task UploadAsync(
                string storageKey,
                Stream content,
                string contentType,
                long contentLength,
                CancellationToken cancellationToken = default
            )
            {
                using var buffer = new MemoryStream();
                await content.CopyToAsync(buffer, cancellationToken);
                _files[storageKey] = buffer.ToArray();
            }

            public Task<Stream> OpenReadAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult<Stream>(
                    new MemoryStream(GetRequired(storageKey))
                );

            public Task DeleteAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            )
            {
                _files.TryRemove(storageKey, out _);
                return Task.CompletedTask;
            }
        }
    }
}
