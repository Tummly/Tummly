using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.PrintReadyQrMaterials;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public sealed class PrintReadyQrMaterialsServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public PrintReadyQrMaterialsServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
        }

        [Fact]
        public async Task EnsureStarterMaterials_RetriesOneTransientStorageFailure()
        {
            var location = await SeedLocationWithTableTentAsync();
            var storage = new FailFirstUploadStorage();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example",
                })
                .Build();
            var guestLinks = new SmartGuestLinkService(
                _context,
                configuration,
                new NoOpBillingAccountLifecycle()
            );
            var service = new PrintReadyQrMaterialsService(
                _context,
                PrintTemplatePack.LoadFromContentRoot(AppContext.BaseDirectory),
                storage,
                new QrCoderRasterizer(),
                guestLinks,
                NullLogger<PrintReadyQrMaterialsService>.Instance
            );

            await service.EnsureStarterMaterialsAsync(location.Id);

            var asset = await _context.PrintReadyQrAssets.SingleAsync();
            Assert.Equal(2, storage.UploadAttempts);
            Assert.Equal(PrintReadyQrAssetStatus.Ready, asset.Status);
            Assert.Null(asset.LastError);
        }

        [Fact]
        public async Task EnsureStarterMaterials_LeavesMatchingReadyAssetUnchanged()
        {
            var location = await SeedLocationWithTableTentAsync();
            var qrCode = await _context.QrCodes.SingleAsync();
            var storage = new FailFirstUploadStorage();
            var pack = PrintTemplatePack.LoadFromContentRoot(
                AppContext.BaseDirectory
            );
            var fingerprint = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(qrCode.Token)
                )
            ).ToLowerInvariant();
            var originalUpdatedAt = DateTime.UtcNow.AddHours(-1);
            _context.PrintReadyQrAssets.Add(new PrintReadyQrAsset
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.TableTent,
                Status = PrintReadyQrAssetStatus.Ready,
                StorageKey = $"ready/{fingerprint}.pdf",
                FileName = "ready.pdf",
                QrTokenFingerprint = fingerprint,
                TemplatePackVersion = pack.CurrentPackId,
                OfferCopyVersion = pack.Snapshot.OfferCopyVersion,
                CreatedAtUtc = originalUpdatedAt,
                UpdatedAtUtc = originalUpdatedAt,
            });
            await _context.SaveChangesAsync();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example",
                })
                .Build();
            var service = new PrintReadyQrMaterialsService(
                _context,
                pack,
                storage,
                new QrCoderRasterizer(),
                new SmartGuestLinkService(
                    _context,
                    configuration,
                    new NoOpBillingAccountLifecycle()
                ),
                NullLogger<PrintReadyQrMaterialsService>.Instance
            );

            await service.EnsureStarterMaterialsAsync(location.Id);
            await service.EnsureStarterMaterialsAsync(location.Id);

            var asset = await _context.PrintReadyQrAssets.SingleAsync();
            Assert.Equal(0, storage.UploadAttempts);
            Assert.Equal(PrintReadyQrAssetStatus.Ready, asset.Status);
            Assert.Equal(originalUpdatedAt, asset.UpdatedAtUtc);
        }

        [Fact]
        public void OfferCardPdf_UsesSvgPointCanvasAndIncludesTemplateArtwork()
        {
            var pack = PrintTemplatePack.LoadFromContentRoot(
                AppContext.BaseDirectory
            );
            var (widthPt, heightPt) = PrintTemplatePack.ReadSvgViewBoxPoints(
                pack.Snapshot.CardSvgPath
            );
            var qrPlacement =
                PrintReadyQrPdfComposer.ResolveQrPlacementPoints(
                    widthPt,
                    heightPt,
                    pack.Snapshot.OfferCardQr
                );
            var whiteQr = new QrRasterImage(
                3,
                3,
                Enumerable.Repeat((byte)255, 27).ToArray()
            );

            var pdf = PrintReadyQrPdfComposer.Compose(
                pack.Snapshot,
                QrType.OfferCard,
                whiteQr,
                pack.Snapshot.DefaultOfferHeadline
            );

            Assert.Equal(240.94f, widthPt, 2);
            Assert.Equal(155.91f, heightPt, 2);
            Assert.Equal(176.09, qrPlacement.XPt, 2);
            Assert.Equal(13.63, qrPlacement.YPt, 2);
            Assert.Equal(51.02, qrPlacement.WidthPt, 2);
            Assert.Equal(51.02, qrPlacement.HeightPt, 2);
            Assert.Equal(7, pack.Snapshot.OfferCardHeadline.XMm);
            Assert.Equal(7, pack.Snapshot.OfferCardHeadline.YMm);
            Assert.Equal(43, pack.Snapshot.OfferCardHeadline.WidthMm);
            Assert.Equal(18, pack.Snapshot.OfferCardHeadline.HeightMm);
            Assert.True(
                pdf.Length > 10_000,
                "The PDF must include the Card Dev SVG vector artwork."
            );
            Assert.Contains(
                pack.Snapshot.DefaultOfferHeadline,
                System.Text.Encoding.ASCII.GetString(pdf)
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<RestaurantLocation> SeedLocationWithTableTentAsync()
        {
            var owner = new User
            {
                FullName = "Print owner",
                Email = $"print-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900999",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
            };
            var restaurant = new Restaurant
            {
                Name = "Print venue",
                OwnerUser = owner,
                CreatedAt = DateTime.UtcNow,
            };
            var location = new RestaurantLocation
            {
                Restaurant = restaurant,
                LocationName = "Main",
                Address = "1 High Street",
                City = "London",
                Postcode = "SE1 1AA",
                CreatedAt = DateTime.UtcNow,
            };
            _context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocation = location,
                    QrType = QrType.TableTent,
                    Token = "transient-storage-token-123456",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();
            return location;
        }

        private sealed class FailFirstUploadStorage : IQueryAttachmentStorage
        {
            private byte[]? _stored;

            public bool IsConfigured => true;

            public int UploadAttempts { get; private set; }

            public async Task UploadAsync(
                string storageKey,
                Stream content,
                string contentType,
                long contentLength,
                CancellationToken cancellationToken = default
            )
            {
                UploadAttempts++;
                if (UploadAttempts == 1)
                {
                    throw new IOException("Transient storage failure.");
                }

                using var buffer = new MemoryStream();
                await content.CopyToAsync(buffer, cancellationToken);
                _stored = buffer.ToArray();
            }

            public Task<Stream> OpenReadAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult<Stream>(
                    new MemoryStream(
                        _stored ?? throw new FileNotFoundException(storageKey)
                    )
                );
            }

            public Task DeleteAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            )
            {
                _stored = null;
                return Task.CompletedTask;
            }
        }
    }
}
