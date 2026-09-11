using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.PrintReadyQrMaterials;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;
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
                MaterialsCatalog.LoadFromContentRoot(AppContext.BaseDirectory),
                storage,
                new QrCoderRasterizer(),
                guestLinks,
                new NoOpPrintReadyQrMaterialsWork(),
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
            var storageKey = $"ready/{fingerprint}.pdf";
            storage.Seed(storageKey, [1, 2, 3]);
            _context.PrintReadyQrAssets.Add(new PrintReadyQrAsset
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.TableTent,
                Status = PrintReadyQrAssetStatus.Ready,
                StorageKey = storageKey,
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
                MaterialsCatalog.LoadFromContentRoot(AppContext.BaseDirectory),
                storage,
                new QrCoderRasterizer(),
                new SmartGuestLinkService(
                    _context,
                    configuration,
                    new NoOpBillingAccountLifecycle()
                ),
                new NoOpPrintReadyQrMaterialsWork(),
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
        public async Task EnsureStarterMaterials_MatchingMetadataButMissingPdf_Regenerates()
        {
            var location = await SeedLocationWithTableTentAsync();
            var qrCode = await _context.QrCodes.SingleAsync();
            var pack = PrintTemplatePack.LoadFromContentRoot(
                AppContext.BaseDirectory
            );
            var fingerprint = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(qrCode.Token)
                )
            ).ToLowerInvariant();
            _context.PrintReadyQrAssets.Add(new PrintReadyQrAsset
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.TableTent,
                Status = PrintReadyQrAssetStatus.Ready,
                StorageKey = "missing/table-tent.pdf",
                FileName = "table-tent.pdf",
                QrTokenFingerprint = fingerprint,
                TemplatePackVersion = pack.CurrentPackId,
                OfferCopyVersion = pack.Snapshot.OfferCopyVersion,
            });
            await _context.SaveChangesAsync();
            var storage = new RecordingStorage();
            var service = CreateService(
                storage,
                new NoOpPrintReadyQrMaterialsWork()
            );

            await service.EnsureStarterMaterialsAsync(location.Id);

            var asset = await _context.PrintReadyQrAssets.SingleAsync();
            Assert.Equal(1, storage.UploadAttempts);
            Assert.Equal(PrintReadyQrAssetStatus.Ready, asset.Status);
            Assert.NotEqual("missing/table-tent.pdf", asset.StorageKey);
        }

        [Fact]
        public async Task Download_MissingStoredPdf_MarksAssetFailed()
        {
            var location = await SeedLocationWithTableTentAsync();
            _context.PrintReadyQrAssets.Add(new PrintReadyQrAsset
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.TableTent,
                Status = PrintReadyQrAssetStatus.Ready,
                StorageKey = "missing/download.pdf",
                FileName = "table-tent.pdf",
            });
            await _context.SaveChangesAsync();
            var service = CreateService(
                new RecordingStorage(),
                new NoOpPrintReadyQrMaterialsWork()
            );

            var exception = await Assert.ThrowsAsync<
                PrintReadyQrNotReadyException
            >(() =>
                service.DownloadAsync(
                    location.Restaurant!.OwnerUserId,
                    location.Id,
                    QrType.TableTent
                )
            );

            Assert.Equal(PrintReadyQrAssetStatus.Failed, exception.Status);
            var asset = await _context.PrintReadyQrAssets.SingleAsync();
            Assert.Equal(PrintReadyQrAssetStatus.Failed, asset.Status);
            Assert.Contains("missing or unavailable", asset.LastError);
        }

        [Fact]
        public async Task EnsureShopOrderMaterials_ReusesOnlyWhenTokenTemplateAndOfferVersionsMatch()
        {
            var location = await SeedLocationWithTableTentAsync();
            var storage = new RecordingStorage();
            var pack = PrintTemplatePack.LoadFromContentRoot(
                AppContext.BaseDirectory
            );
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example",
                })
                .Build();
            var service = new PrintReadyQrMaterialsService(
                _context,
                pack,
                MaterialsCatalog.LoadFromContentRoot(AppContext.BaseDirectory),
                storage,
                new QrCoderRasterizer(),
                new SmartGuestLinkService(
                    _context,
                    configuration,
                    new NoOpBillingAccountLifecycle()
                ),
                new NoOpPrintReadyQrMaterialsWork(),
                NullLogger<PrintReadyQrMaterialsService>.Instance
            );

            var firstOrderId = await SeedPaidOrderAsync(location, quantity: 25);
            await service.EnsureShopOrderMaterialsAsync(firstOrderId);
            Assert.Equal(1, storage.UploadAttempts);

            var reorderId = await SeedPaidOrderAsync(location, quantity: 100);
            await service.EnsureShopOrderMaterialsAsync(reorderId);
            Assert.Equal(1, storage.UploadAttempts);
            var reused = await _context.PrintReadyQrAssets
                .Where(row =>
                    row.ShopOrderId == firstOrderId
                    || row.ShopOrderId == reorderId
                )
                .OrderBy(row => row.ShopOrderId)
                .ToListAsync();
            Assert.Equal(2, reused.Count);
            Assert.Equal(reused[0].StorageKey, reused[1].StorageKey);
            Assert.All(
                reused,
                row => Assert.Equal(
                    PrintReadyQrAssetStatus.Ready,
                    row.Status
                )
            );
            var readiness = await service.ListShopOrderReadinessAsync(reorderId);
            Assert.Equal(100, Assert.Single(readiness).Quantity);

            foreach (var asset in await _context.PrintReadyQrAssets.ToListAsync())
            {
                asset.TemplatePackVersion = "older-template";
            }
            await _context.SaveChangesAsync();
            var templateChangedOrderId = await SeedPaidOrderAsync(
                location,
                quantity: 5
            );
            await service.EnsureShopOrderMaterialsAsync(templateChangedOrderId);
            Assert.Equal(2, storage.UploadAttempts);

            foreach (var asset in await _context.PrintReadyQrAssets.ToListAsync())
            {
                asset.TemplatePackVersion = pack.CurrentPackId;
                asset.OfferCopyVersion = "older-offer-copy";
            }
            await _context.SaveChangesAsync();
            var offerChangedOrderId = await SeedPaidOrderAsync(
                location,
                quantity: 6
            );
            await service.EnsureShopOrderMaterialsAsync(offerChangedOrderId);
            Assert.Equal(3, storage.UploadAttempts);

            foreach (var asset in await _context.PrintReadyQrAssets.ToListAsync())
            {
                asset.TemplatePackVersion = pack.CurrentPackId;
                asset.OfferCopyVersion = pack.Snapshot.OfferCopyVersion;
            }
            var qrCode = await _context.QrCodes.SingleAsync();
            qrCode.Token = "rotated-shop-token-654321";
            await _context.SaveChangesAsync();
            var rotatedOrderId = await SeedPaidOrderAsync(
                location,
                quantity: 7
            );
            await service.EnsureShopOrderMaterialsAsync(rotatedOrderId);

            Assert.Equal(4, storage.UploadAttempts);
            Assert.Equal(1, await _context.QrCodes.CountAsync());
            Assert.Equal(
                5,
                await _context.PrintReadyQrAssets.CountAsync(row =>
                    row.ShopOrderId != null
                )
            );
        }

        [Fact]
        public async Task EnsureStarterMaterials_MissingEligibleQr_MarksFailed()
        {
            var location = await SeedLocationWithTableTentAsync();
            _context.QrCodes.RemoveRange(_context.QrCodes);
            await _context.SaveChangesAsync();
            var storage = new RecordingStorage();
            var service = CreateService(
                storage,
                new NoOpPrintReadyQrMaterialsWork()
            );

            await service.EnsureStarterMaterialsAsync(location.Id);

            var assets = await _context.PrintReadyQrAssets.ToListAsync();
            Assert.Equal(3, assets.Count);
            Assert.All(
                assets,
                asset =>
                {
                    Assert.Equal(PrintReadyQrAssetStatus.Failed, asset.Status);
                    Assert.Contains(
                        "No Active or Paused QR code",
                        asset.LastError
                    );
                }
            );
            Assert.Equal(0, storage.UploadAttempts);
        }

        [Fact]
        public async Task EnsureShopOrderMaterials_PausedCode_FailsWithoutGenerating()
        {
            var location = await SeedLocationWithTableTentAsync();
            var qrCode = await _context.QrCodes.SingleAsync();
            qrCode.Status = QrCodeStatus.Paused;
            await _context.SaveChangesAsync();
            var storage = new RecordingStorage();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example",
                })
                .Build();
            var service = new PrintReadyQrMaterialsService(
                _context,
                PrintTemplatePack.LoadFromContentRoot(AppContext.BaseDirectory),
                MaterialsCatalog.LoadFromContentRoot(AppContext.BaseDirectory),
                storage,
                new QrCoderRasterizer(),
                new SmartGuestLinkService(
                    _context,
                    configuration,
                    new NoOpBillingAccountLifecycle()
                ),
                new NoOpPrintReadyQrMaterialsWork(),
                NullLogger<PrintReadyQrMaterialsService>.Instance
            );
            var orderId = await SeedPaidOrderAsync(location, quantity: 1);

            await service.EnsureShopOrderMaterialsAsync(orderId);

            var asset = await _context.PrintReadyQrAssets.SingleAsync(row =>
                row.ShopOrderId == orderId
            );
            Assert.Equal(PrintReadyQrAssetStatus.Failed, asset.Status);
            Assert.Contains("No Active QR code", asset.LastError);
            Assert.Equal(0, storage.UploadAttempts);
        }

        [Fact]
        public void ResolveOrderedQrTypes_UsesStableSkuSnapshotWhenCurrentCatalogRemovedSku()
        {
            var types = PrintReadyQrMaterialsService.ResolveOrderedQrTypes(
                [
                    new ShopOrderLine
                    {
                        CatalogSkuId = "table-tents",
                        Quantity = 25,
                    },
                ],
                new EmptyMaterialsCatalog()
            );

            Assert.Equal(25, types[QrType.TableTent]);
        }

        [Fact]
        public async Task InvalidateAfterQrRotation_InvalidatesAndQueuesEveryAssetScope()
        {
            var location = await SeedLocationWithTableTentAsync();
            var shopOrderId = Guid.NewGuid();
            _context.PrintReadyQrAssets.AddRange(
                ReadyAsset(shopOrderId: null),
                ReadyAsset(shopOrderId)
            );
            await _context.SaveChangesAsync();
            var work = new RecordingPrintReadyQrMaterialsWork();
            var service = CreateService(new RecordingStorage(), work);

            Assert.True(
                await service.TryInvalidateAfterQrRotationAsync(
                    location.Id,
                    QrType.TableTent
                )
            );

            var assets = await _context.PrintReadyQrAssets.ToListAsync();
            Assert.Equal(2, assets.Count);
            Assert.All(assets, asset =>
            {
                Assert.Equal(PrintReadyQrAssetStatus.Preparing, asset.Status);
                Assert.Null(asset.StorageKey);
                Assert.Null(asset.FileName);
                Assert.Null(asset.QrTokenFingerprint);
                Assert.Null(asset.TemplatePackVersion);
                Assert.Null(asset.OfferCopyVersion);
                Assert.Null(asset.LastError);
            });
            Assert.Equal(new[] { location.Id }, work.LocationIds);
            Assert.Equal(new[] { shopOrderId }, work.ShopOrderIds);

            PrintReadyQrAsset ReadyAsset(Guid? shopOrderId) =>
                new()
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.TableTent,
                    ShopOrderId = shopOrderId,
                    Status = PrintReadyQrAssetStatus.Ready,
                    StorageKey = "old.pdf",
                    FileName = "old.pdf",
                    QrTokenFingerprint = "old-token",
                    TemplatePackVersion = "old-pack",
                    OfferCopyVersion = "old-copy",
                };
        }

        [Fact]
        public async Task InvalidateAfterQrRotation_QueueFailure_MarksAffectedAssetFailed()
        {
            var location = await SeedLocationWithTableTentAsync();
            var qrCode = await _context.QrCodes.SingleAsync();
            qrCode.Token = "rotated-atomic-token-1234567890";
            _context.PrintReadyQrAssets.Add(new PrintReadyQrAsset
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.TableTent,
                Status = PrintReadyQrAssetStatus.Ready,
                StorageKey = "old.pdf",
                FileName = "old.pdf",
                QrTokenFingerprint = "old-token",
                TemplatePackVersion = "old-pack",
                OfferCopyVersion = "old-copy",
            });
            await _context.SaveChangesAsync();
            var work = new RecordingPrintReadyQrMaterialsWork
            {
                ThrowOnStarterRequest = true,
            };
            var service = CreateService(new RecordingStorage(), work);

            Assert.True(
                await service.TryInvalidateAfterQrRotationAsync(
                    location.Id,
                    QrType.TableTent
                )
            );

            _context.ChangeTracker.Clear();
            var asset = await _context.PrintReadyQrAssets.SingleAsync();
            Assert.Equal(PrintReadyQrAssetStatus.Failed, asset.Status);
            Assert.Contains("Could not queue regeneration", asset.LastError);
            Assert.Equal(
                "rotated-atomic-token-1234567890",
                await _context.QrCodes
                    .Select(row => row.Token)
                    .SingleAsync()
            );
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
            var pdfWithoutHeadline = PrintReadyQrPdfComposer.Compose(
                pack.Snapshot,
                QrType.OfferCard,
                whiteQr,
                string.Empty
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
            Assert.True(
                pdf.Length > pdfWithoutHeadline.Length + 500,
                "The offer headline must add visible vector geometry, not metadata only."
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

        private PrintReadyQrMaterialsService CreateService(
            IQueryAttachmentStorage storage,
            IPrintReadyQrMaterialsWork work
        )
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example",
                })
                .Build();
            return new PrintReadyQrMaterialsService(
                _context,
                PrintTemplatePack.LoadFromContentRoot(AppContext.BaseDirectory),
                MaterialsCatalog.LoadFromContentRoot(AppContext.BaseDirectory),
                storage,
                new QrCoderRasterizer(),
                new SmartGuestLinkService(
                    _context,
                    configuration,
                    new NoOpBillingAccountLifecycle()
                ),
                work,
                NullLogger<PrintReadyQrMaterialsService>.Instance
            );
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

        private async Task<Guid> SeedPaidOrderAsync(
            RestaurantLocation location,
            int quantity
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .SingleAsync(row => row.Id == location.RestaurantId);
            var order = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{Guid.NewGuid():N}"[..20],
                RestaurantId = restaurant.Id,
                LocationId = location.Id,
                LocationNameSnapshot = location.LocationName,
                PlacedByUserId = restaurant.OwnerUserId,
                PlacedByNameSnapshot = "Print owner",
                MaterialsNetPence = 2400 * quantity,
                VatPence = 480 * quantity,
                GrossPence = 2880 * quantity,
                DeliveryMethod = ShopDeliveryMethods.Standard,
                PaymentStatus = ShopPaymentStatuses.Paid,
                FulfilmentStatus = ShopFulfilmentStatuses.Processing,
                PaidAtUtc = DateTime.UtcNow,
                ProcessingStartedAtUtc = DateTime.UtcNow,
                ShipToContactName = "Print owner",
                ShipToAddressLine1 = "1 High Street",
                ShipToPostcode = "SE1 1AA",
                ShipToCountry = "United Kingdom",
                Lines =
                {
                    new ShopOrderLine
                    {
                        Id = Guid.NewGuid(),
                        CatalogSkuId = "table-tents",
                        TitleSnapshot = "Table Tent QR",
                        MaterialType = "tabletop",
                        Quantity = quantity,
                        UnitNetPence = 2400,
                        LineNetPence = 2400 * quantity,
                    },
                },
            };
            _context.ShopOrders.Add(order);
            await _context.SaveChangesAsync();
            return order.Id;
        }

        private sealed class RecordingPrintReadyQrMaterialsWork
            : IPrintReadyQrMaterialsWork
        {
            public List<int> LocationIds { get; } = [];

            public List<Guid> ShopOrderIds { get; } = [];

            public bool ThrowOnStarterRequest { get; set; }

            public ValueTask RequestEnsureAsync(
                int locationId,
                CancellationToken cancellationToken = default
            )
            {
                if (ThrowOnStarterRequest)
                {
                    throw new InvalidOperationException(
                        "Controlled queue failure."
                    );
                }

                LocationIds.Add(locationId);
                return ValueTask.CompletedTask;
            }

            public ValueTask RequestShopOrderEnsureAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            )
            {
                ShopOrderIds.Add(shopOrderId);
                return ValueTask.CompletedTask;
            }

            public Task RunAsync(CancellationToken stoppingToken) =>
                Task.CompletedTask;

            public Task DrainAsync(
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }

        private sealed class NoOpPrintReadyQrMaterialsWork
            : IPrintReadyQrMaterialsWork
        {
            public ValueTask RequestEnsureAsync(
                int locationId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public ValueTask RequestShopOrderEnsureAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public Task RunAsync(CancellationToken stoppingToken) =>
                Task.CompletedTask;

            public Task DrainAsync(
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }

        private sealed class RecordingStorage : IQueryAttachmentStorage
        {
            private readonly Dictionary<string, byte[]> _objects = [];

            public bool IsConfigured => true;

            public int UploadAttempts { get; private set; }

            public void Seed(string storageKey, byte[] content)
            {
                _objects[storageKey] = content;
            }

            public async Task UploadAsync(
                string storageKey,
                Stream content,
                string contentType,
                long contentLength,
                CancellationToken cancellationToken = default
            )
            {
                UploadAttempts++;
                using var buffer = new MemoryStream();
                await content.CopyToAsync(buffer, cancellationToken);
                _objects[storageKey] = buffer.ToArray();
            }

            public Task<Stream> OpenReadAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult<Stream>(
                    new MemoryStream(_objects[storageKey])
                );
            }

            public Task DeleteAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            )
            {
                _objects.Remove(storageKey);
                return Task.CompletedTask;
            }
        }

        private sealed class FailFirstUploadStorage : IQueryAttachmentStorage
        {
            private byte[]? _stored;

            public bool IsConfigured => true;

            public int UploadAttempts { get; private set; }

            public void Seed(string storageKey, byte[] content)
            {
                _stored = content;
            }

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

        private sealed class EmptyMaterialsCatalog : IMaterialsCatalog
        {
            public string CurrentCatalogId => "empty";

            public MaterialsCatalogSnapshot GetRequired(string catalogId) =>
                new()
                {
                    Id = catalogId,
                    Skus = [],
                };

            public IReadOnlyList<
                TummlyBackend.DTOs.Shop.ShopCatalogListItemDto
            > BuildList() => [];

            public TummlyBackend.DTOs.Shop.ShopCatalogDetailDto? TryBuildDetail(
                string skuId
            ) => null;
        }
    }
}
