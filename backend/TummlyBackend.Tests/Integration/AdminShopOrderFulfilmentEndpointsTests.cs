using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class AdminShopOrderFulfilmentEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminShopOrderFulfilmentEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListShopOrders_DefaultsToAllStatuses_AndExposesRestaurantNameAndCreatedAt()
        {
            var seeded = await SeedWorkspaceAsync();
            await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing
            );
            await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/1"
            );

            // Scope to this workspace: the in-memory store is shared across tests.
            using var request = AuthorizedGet(
                $"/api/admin/shop-orders?restaurantId={seeded.RestaurantId}",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("pageSize").GetInt32());
            var items = body.GetProperty("items").EnumerateArray().ToList();
            Assert.Equal(
                new[]
                {
                    ShopFulfilmentStatuses.InTransit,
                    ShopFulfilmentStatuses.Processing,
                },
                items
                    .Select(item => item.GetProperty("fulfilmentStatus").GetString()!)
                    .OrderBy(status => status)
            );
            var first = items[0];
            Assert.True(first.TryGetProperty("opsNotes", out _));
            Assert.True(first.TryGetProperty("revolutOrderId", out _));
            Assert.False(
                string.IsNullOrWhiteSpace(first.GetProperty("restaurantName").GetString())
            );
            var createdAt = first.GetProperty("createdAtUtc").GetString();
            Assert.EndsWith("Z", createdAt);
        }

        [Fact]
        public async Task ListShopOrders_Returns403_ForOperatorJwt()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedGet(
                "/api/admin/shop-orders",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListShopOrders_CounterDisplayAsset_CarriesProductTitleNotOfferCardMapping()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                catalogSkuId: ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                titleSnapshot: "Offer Card",
                quantity: 10
            );
            using (var scope = _factory.Services.CreateScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<IPrintReadyQrMaterialsService>()
                    .EnsureShopOrderMaterialsAsync(orderId);
            }

            using var list = AuthorizedGet(
                $"/api/admin/shop-orders?restaurantId={seeded.RestaurantId}",
                seeded.AdminJwt
            );
            var listResponse = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var listBody = await ReadJsonAsync(listResponse);
            var asset = Assert.Single(
                listBody.GetProperty("items")[0]
                    .GetProperty("printAssets")
                    .EnumerateArray()
            );

            Assert.Equal("OfferCard", asset.GetProperty("qrType").GetString());
            Assert.Equal(
                "QR Counter Display",
                asset.GetProperty("productTitle").GetString()
            );
        }

        [Fact]
        public async Task ListShopOrders_ReturnsReadiness_AndDownloadsPatchCsvAfterApprove()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                quantity: 40
            );
            using (var scope = _factory.Services.CreateScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<IPrintReadyQrMaterialsService>()
                    .EnsureShopOrderMaterialsAsync(orderId);
            }

            using var list = AuthorizedGet(
                $"/api/admin/shop-orders?restaurantId={seeded.RestaurantId}",
                seeded.AdminJwt
            );
            var listResponse = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var listBody = await ReadJsonAsync(listResponse);
            var asset = Assert.Single(
                listBody.GetProperty("items")[0]
                    .GetProperty("printAssets")
                    .EnumerateArray()
            );
            Assert.Equal("TableTent", asset.GetProperty("qrType").GetString());
            Assert.Equal(40, asset.GetProperty("quantity").GetInt32());
            Assert.Equal("Ready", asset.GetProperty("status").GetString());
            Assert.False(asset.GetProperty("approved").GetBoolean());

            using var beforeApprove = AuthorizedGet(
                $"/api/admin/shop-orders/{orderId}/print-assets/TableTent/download",
                seeded.AdminJwt
            );
            var beforeResponse = await _client.SendAsync(beforeApprove);
            Assert.Equal(HttpStatusCode.Conflict, beforeResponse.StatusCode);
            var beforeBody = await ReadJsonAsync(beforeResponse);
            Assert.Equal(
                "print_asset_not_approved",
                beforeBody.GetProperty("code").GetString()
            );

            using var approve = AuthorizedPost(
                $"/api/admin/shop-orders/{orderId}/print-assets/TableTent/approve",
                seeded.AdminJwt
            );
            var approveResponse = await _client.SendAsync(approve);
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
            var approveBody = await ReadJsonAsync(approveResponse);
            Assert.True(approveBody.GetProperty("approved").GetBoolean());

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var audit = Assert.Single(
                    await context.AdminAuditEvents
                        .Where(row =>
                            row.Action
                                == AdminAuditActions.ShopPrintAssetApproved
                            && row.TargetId == orderId.ToString("D")
                        )
                        .ToListAsync()
                );
                Assert.Equal(
                    AdminAuditTargetTypes.ShopOrder,
                    audit.TargetType
                );
            }

            using var download = AuthorizedGet(
                $"/api/admin/shop-orders/{orderId}/print-assets/TableTent/download",
                seeded.AdminJwt
            );
            var downloadResponse = await _client.SendAsync(download);
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
            Assert.Equal(
                "text/csv",
                downloadResponse.Content.Headers.ContentType?.MediaType
            );
            var csv = System.Text.Encoding.UTF8.GetString(
                await downloadResponse.Content.ReadAsByteArrayAsync()
            );
            Assert.Contains(
                "JobId,OrderId,LocationId,LocationName,PlacementId,PlacementType,QRURL,PrtQty",
                csv
            );
            Assert.Contains(",40\r\n", csv);
            Assert.DoesNotContain("OfferHeadline", csv);
        }

        [Fact]
        public async Task CounterDisplay_EmbeddedPdf_DownloadsAfterApprove()
        {
            // Ticket 04 binds QR Counter Display to OfferCard placement + EmbeddedPdf.
            // Counter Display never needs a card offer, so its PDF generates
            // without one. Approval is still required before download.
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                catalogSkuId: "qr-counter-display",
                titleSnapshot: "QR Counter Display",
                quantity: 2
            );
            using (var scope = _factory.Services.CreateScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<IPrintReadyQrMaterialsService>()
                    .EnsureShopOrderMaterialsAsync(orderId);
            }

            using var beforeApprove = AuthorizedGet(
                $"/api/admin/shop-orders/{orderId}/print-assets/OfferCard/download",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.Conflict,
                (await _client.SendAsync(beforeApprove)).StatusCode
            );

            using var approve = AuthorizedPost(
                $"/api/admin/shop-orders/{orderId}/print-assets/OfferCard/approve",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(approve)).StatusCode
            );

            using var download = AuthorizedGet(
                $"/api/admin/shop-orders/{orderId}/print-assets/OfferCard/download",
                seeded.AdminJwt
            );
            var downloadResponse = await _client.SendAsync(download);
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
            Assert.Equal(
                "application/pdf",
                downloadResponse.Content.Headers.ContentType?.MediaType
            );
            var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 4);
            Assert.Equal((byte)'%', bytes[0]);
            Assert.Equal((byte)'P', bytes[1]);
            Assert.Equal((byte)'D', bytes[2]);
            Assert.Equal((byte)'F', bytes[3]);
        }

        [Fact]
        public async Task CounterDisplay_PaidOrderPdf_ReadyWithoutAttachedOffer()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                catalogSkuId: "qr-counter-display",
                titleSnapshot: "QR Counter Display",
                quantity: 2
            );

            await EnsureShopMaterialsAsync(orderId);

            var counter = Assert.Single(await ListShopReadinessAsync(orderId));
            Assert.Equal("OfferCard", counter.QrType);
            Assert.Equal("Ready", counter.Status);
        }

        [Fact]
        public async Task StarterKit_CounterPdf_ReadyWithoutAttachedOffer()
        {
            var seeded = await SeedWorkspaceAsync();
            Guid orderId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var billing = await context.BillingAccounts
                    .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
                billing.SubscriptionPlan = BillingSubscriptionPlans.Starter;
                await context.SaveChangesAsync();

                var starter = await scope.ServiceProvider
                    .GetRequiredService<IComplimentaryStarterShopOrderService>()
                    .EnsureForLocationAsync(
                        seeded.RestaurantId,
                        seeded.LocationId,
                        seeded.OwnerUserId,
                        "Admin Shop Owner"
                    );
                Assert.True(starter.Created);
                orderId = starter.ShopOrderId;
            }

            await EnsureShopMaterialsAsync(orderId);

            var counter = Assert.Single(await ListShopReadinessAsync(orderId));
            Assert.Equal("OfferCard", counter.QrType);
            Assert.Equal("Ready", counter.Status);
        }

        [Fact]
        public async Task GuestCard_ShopOrderAsset_StaysPreparingWithoutOffer()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                catalogSkuId: "qr-guest-cards",
                titleSnapshot: "QR Guest Cards",
                quantity: 250
            );

            await EnsureShopMaterialsAsync(orderId);

            var guestCards = Assert.Single(await ListShopReadinessAsync(orderId));
            Assert.Equal("GuestCard", guestCards.QrType);
            Assert.Equal("Preparing", guestCards.Status);
        }

        [Fact]
        public async Task GuestCard_ShopOrderAsset_StaysPreparingWhenAttachedOfferExpired()
        {
            var seeded = await SeedWorkspaceAsync();
            await AddActiveGuestCardQrAsync(seeded.LocationId);
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                // Attached while live, expired since: checkout would refuse it.
                var offer = new CatalogOffer
                {
                    RestaurantLocationId = seeded.LocationId,
                    Title = "Expired Dessert Friday",
                    Description = "Guest card offer",
                    Status = CatalogOfferStatus.Active,
                    Validity = CatalogOfferValidity.ChooseExpiryDate,
                    CustomExpiryDate = DateOnly.FromDateTime(
                        DateTime.UtcNow.AddDays(-3)
                    ),
                    OfferType = CatalogOfferType.FreeItem,
                    FreeItemText = "Dessert",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                context.CatalogOffers.Add(offer);
                await context.SaveChangesAsync();
                var location = await context.RestaurantLocations
                    .SingleAsync(row => row.Id == seeded.LocationId);
                location.OfferCardCatalogOfferId = offer.Id;
                await context.SaveChangesAsync();
            }

            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                catalogSkuId: "qr-guest-cards",
                titleSnapshot: "QR Guest Cards",
                quantity: 12
            );

            await EnsureShopMaterialsAsync(orderId);

            var guestCards = Assert.Single(await ListShopReadinessAsync(orderId));
            Assert.Equal("GuestCard", guestCards.QrType);
            Assert.Equal("Preparing", guestCards.Status);
        }

        [Fact]
        public async Task GuestCard_ShopOrderAsset_StaysPreparingWhenAttachedOfferBelongsToAnotherLocation()
        {
            var seeded = await SeedWorkspaceAsync();
            var otherLocationId = await ShopTestSeed.AddLocationAsync(
                _factory,
                seeded.RestaurantId,
                "Other Site"
            );
            await AddActiveGuestCardQrAsync(seeded.LocationId);
            await ShopTestSeed.AttachCardOfferAsync(
                _factory,
                seeded.LocationId,
                new ShopTestSeed.CardOfferSetup(OfferLocationId: otherLocationId)
            );
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                catalogSkuId: "qr-guest-cards",
                titleSnapshot: "QR Guest Cards",
                quantity: 12
            );

            await EnsureShopMaterialsAsync(orderId);

            var guestCards = Assert.Single(await ListShopReadinessAsync(orderId));
            Assert.Equal("GuestCard", guestCards.QrType);
            Assert.Equal("Preparing", guestCards.Status);
        }

        [Fact]
        public async Task GuestCard_ShopOrderAsset_BecomesReadyWithOwnLocationOffer()
        {
            // Positive control for the test above: same QR and order, offer at
            // this location, so the gate opens and the asset becomes Ready.
            var seeded = await SeedWorkspaceAsync();
            await AddActiveGuestCardQrAsync(seeded.LocationId);
            await ShopTestSeed.AttachActiveCardOfferAsync(_factory, seeded.LocationId);
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                catalogSkuId: "qr-guest-cards",
                titleSnapshot: "QR Guest Cards",
                quantity: 12
            );

            await EnsureShopMaterialsAsync(orderId);

            var guestCards = Assert.Single(await ListShopReadinessAsync(orderId));
            Assert.Equal("GuestCard", guestCards.QrType);
            Assert.Equal("Ready", guestCards.Status);
        }

        private async Task AddActiveGuestCardQrAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = locationId,
                    QrType = QrType.GuestCard,
                    Token = $"admin-shop-guest-{Guid.NewGuid():N}",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GuestCardCsv_IncludesOfferHeadline_AfterApprove()
        {
            var seeded = await SeedWorkspaceAsync();
            await AddActiveGuestCardQrAsync(seeded.LocationId);

            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                catalogSkuId: "qr-guest-cards",
                titleSnapshot: "QR Guest Cards",
                quantity: 12
            );
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var location = await context.RestaurantLocations
                    .SingleAsync(row => row.Id == seeded.LocationId);
                var offer = new CatalogOffer
                {
                    RestaurantLocationId = seeded.LocationId,
                    Title = "Free Dessert Friday",
                    Description = "Guest card offer",
                    Status = CatalogOfferStatus.Active,
                    Validity = CatalogOfferValidity.Days30AfterIssue,
                    OfferType = CatalogOfferType.FreeItem,
                    FreeItemText = "Dessert",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                context.CatalogOffers.Add(offer);
                await context.SaveChangesAsync();
                location.OfferCardCatalogOfferId = offer.Id;
                await context.SaveChangesAsync();

                await scope.ServiceProvider
                    .GetRequiredService<IPrintReadyQrMaterialsService>()
                    .EnsureShopOrderMaterialsAsync(orderId);
            }

            using var approve = AuthorizedPost(
                $"/api/admin/shop-orders/{orderId}/print-assets/GuestCard/approve",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(approve)).StatusCode
            );

            using var download = AuthorizedGet(
                $"/api/admin/shop-orders/{orderId}/print-assets/GuestCard/download",
                seeded.AdminJwt
            );
            var downloadResponse = await _client.SendAsync(download);
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
            Assert.Equal(
                "text/csv",
                downloadResponse.Content.Headers.ContentType?.MediaType
            );
            var csv = System.Text.Encoding.UTF8.GetString(
                await downloadResponse.Content.ReadAsByteArrayAsync()
            );
            Assert.Contains("OfferHeadline", csv);
            Assert.Contains("Free Dessert Friday", csv);
            Assert.Contains(",12,", csv);
        }

        [Fact]
        public async Task SoftLock_DoesNotBlockAdminShopDownloadOrRetry()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing
            );
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var account = await context.BillingAccounts
                    .SingleAsync(row =>
                        row.RestaurantId == seeded.RestaurantId
                    );
                account.BillingStatus = BillingStatuses.SoftLock;
                context.PrintReadyQrAssets.Add(new PrintReadyQrAsset
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrType = QrType.TableTent,
                    ShopOrderId = orderId,
                    Status = PrintReadyQrAssetStatus.Failed,
                    LastError = "controlled failure",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                });
                await context.SaveChangesAsync();
            }

            using var retry = AuthorizedPost(
                $"/api/admin/shop-orders/{orderId}/print-assets/TableTent/retry",
                seeded.AdminJwt
            );
            var retryResponse = await _client.SendAsync(retry);
            Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
            Assert.Equal(
                "Ready",
                (await ReadJsonAsync(retryResponse))
                    .GetProperty("status")
                    .GetString()
            );

            using var approve = AuthorizedPost(
                $"/api/admin/shop-orders/{orderId}/print-assets/TableTent/approve",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(approve)).StatusCode
            );

            using var download = AuthorizedGet(
                $"/api/admin/shop-orders/{orderId}/print-assets/TableTent/download",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(download)).StatusCode
            );
        }

        [Fact]
        public async Task Transitions_PreparationProductionDispatchDelivered_SetEvidenceAndAudit()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing
            );

            var prepared = await _client.SendAsync(
                AuthorizedPost(
                    $"/api/admin/shop-orders/{orderId}/preparation-started",
                    seeded.AdminJwt
                )
            );
            Assert.Equal(HttpStatusCode.OK, prepared.StatusCode);
            var afterPrepare = await ReadOrderAsync(orderId);
            Assert.NotNull(afterPrepare.PreparationStartedAtUtc);
            Assert.Equal(seeded.AdminId, afterPrepare.PreparationStartedByAdminUserId);
            Assert.Null(afterPrepare.ProductionStartedAtUtc);

            var produced = await _client.SendAsync(
                AuthorizedPost(
                    $"/api/admin/shop-orders/{orderId}/production-started",
                    seeded.AdminJwt
                )
            );
            Assert.Equal(HttpStatusCode.OK, produced.StatusCode);
            var afterProduction = await ReadOrderAsync(orderId);
            Assert.NotNull(afterProduction.ProductionStartedAtUtc);
            Assert.Equal(
                ShopFulfilmentEvidenceSources.AdminManual,
                afterProduction.ProductionStartSource
            );

            var dispatched = await _client.SendAsync(
                AuthorizedPostJson(
                    $"/api/admin/shop-orders/{orderId}/dispatched",
                    seeded.AdminJwt,
                    new
                    {
                        carrierName = "DPD",
                        carrierReference = "DPD-123",
                        trackingUrl = "https://track.example/abc",
                    }
                )
            );
            Assert.Equal(HttpStatusCode.OK, dispatched.StatusCode);
            var afterDispatch = await ReadOrderAsync(orderId);
            Assert.Equal(ShopFulfilmentStatuses.InTransit, afterDispatch.FulfilmentStatus);
            Assert.NotNull(afterDispatch.DispatchedAtUtc);
            Assert.Equal(seeded.AdminId, afterDispatch.DispatchedByAdminUserId);
            Assert.Equal(ShopFulfilmentEvidenceSources.AdminManual, afterDispatch.DispatchSource);
            Assert.Equal("DPD", afterDispatch.DispatchCarrierName);
            Assert.Equal("DPD-123", afterDispatch.DispatchCarrierReference);
            Assert.Equal("https://track.example/abc", afterDispatch.TrackingUrl);
            Assert.Null(afterDispatch.DeliveredAtUtc);

            using var notes = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new { opsNotes = "handed to carrier" }
            );
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(notes)).StatusCode);
            Assert.Equal("handed to carrier", (await ReadOrderAsync(orderId)).OpsNotes);

            var delivered = await _client.SendAsync(
                AuthorizedPost(
                    $"/api/admin/shop-orders/{orderId}/delivered",
                    seeded.AdminJwt
                )
            );
            Assert.Equal(HttpStatusCode.OK, delivered.StatusCode);
            var afterDeliver = await ReadOrderAsync(orderId);
            Assert.Equal(ShopFulfilmentStatuses.Delivered, afterDeliver.FulfilmentStatus);
            Assert.NotNull(afterDeliver.DeliveredAtUtc);
            Assert.Equal(ShopFulfilmentEvidenceSources.AdminManual, afterDeliver.DeliverySource);
            Assert.Equal("https://track.example/abc", afterDeliver.TrackingUrl);

            foreach (var action in new[]
            {
                AdminAuditActions.ShopPreparationStarted,
                AdminAuditActions.ShopProductionStarted,
                AdminAuditActions.ShopDispatched,
                AdminAuditActions.ShopDelivered,
            })
            {
                Assert.Equal(1, await CountAuditAsync(action, orderId.ToString("D")));
            }
        }
        [Fact]
        public async Task PatchFulfilment_IllegalBackward_Returns409()
        {
            var seeded = await SeedWorkspaceAsync();
            var deliveredId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Delivered,
                deliveredAtUtc: DateTime.UtcNow.AddDays(-1)
            );
            var inTransitId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/x"
            );

            using var backFromDelivered = AuthorizedPatch(
                $"/api/admin/shop-orders/{deliveredId}/fulfilment",
                seeded.AdminJwt,
                new { fulfilmentStatus = ShopFulfilmentStatuses.Processing }
            );
            var deliveredResponse = await _client.SendAsync(backFromDelivered);
            Assert.Equal(HttpStatusCode.Conflict, deliveredResponse.StatusCode);
            var deliveredBody = await ReadJsonAsync(deliveredResponse);
            Assert.False(deliveredBody.GetProperty("success").GetBoolean());
            Assert.False(
                string.IsNullOrWhiteSpace(
                    deliveredBody.GetProperty("code").GetString()
                )
            );

            using var backFromTransit = AuthorizedPatch(
                $"/api/admin/shop-orders/{inTransitId}/fulfilment",
                seeded.AdminJwt,
                new { fulfilmentStatus = ShopFulfilmentStatuses.Processing }
            );
            var transitResponse = await _client.SendAsync(backFromTransit);
            Assert.Equal(HttpStatusCode.Conflict, transitResponse.StatusCode);
        }

        [Fact]
        public async Task Dispatch_RejectsNonHttpsTrackingUrl()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                preparationStartedAtUtc: DateTime.UtcNow.AddHours(-3),
                productionStartedAtUtc: DateTime.UtcNow.AddHours(-2)
            );

            foreach (var badUrl in new[]
            {
                "ftp://files.example/track",
                "http://track.example/insecure",
                "not-a-url",
            })
            {
                using var request = AuthorizedPostJson(
                    $"/api/admin/shop-orders/{orderId}/dispatched",
                    seeded.AdminJwt,
                    new
                    {
                        carrierName = "DPD",
                        carrierReference = "DPD-123",
                        trackingUrl = badUrl,
                    }
                );
                var response = await _client.SendAsync(request);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var body = await ReadJsonAsync(response);
                Assert.Equal("invalid_tracking_url", body.GetProperty("code").GetString());
            }

            var stored = await ReadOrderAsync(orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.DispatchedAtUtc);
            Assert.Null(stored.TrackingUrl);
        }
        [Fact]
        public async Task Dispatch_RequiresCarrierReference_TrackingUrlAloneIsRejected()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                preparationStartedAtUtc: DateTime.UtcNow.AddHours(-3),
                productionStartedAtUtc: DateTime.UtcNow.AddHours(-2)
            );

            using var urlOnly = AuthorizedPostJson(
                $"/api/admin/shop-orders/{orderId}/dispatched",
                seeded.AdminJwt,
                new { trackingUrl = "https://track.example/only" }
            );
            var urlOnlyResponse = await _client.SendAsync(urlOnly);
            Assert.Equal(HttpStatusCode.BadRequest, urlOnlyResponse.StatusCode);
            Assert.Equal(
                "carrier_name_required",
                (await ReadJsonAsync(urlOnlyResponse)).GetProperty("code").GetString()
            );

            using var missingReference = AuthorizedPostJson(
                $"/api/admin/shop-orders/{orderId}/dispatched",
                seeded.AdminJwt,
                new { carrierName = "DPD", trackingUrl = "https://track.example/only" }
            );
            var missingResponse = await _client.SendAsync(missingReference);
            Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode);
            Assert.Equal(
                "carrier_reference_required",
                (await ReadJsonAsync(missingResponse)).GetProperty("code").GetString()
            );

            using var blankReference = AuthorizedPostJson(
                $"/api/admin/shop-orders/{orderId}/dispatched",
                seeded.AdminJwt,
                new { carrierName = "DPD", carrierReference = "   " }
            );
            var blankResponse = await _client.SendAsync(blankReference);
            Assert.Equal(HttpStatusCode.BadRequest, blankResponse.StatusCode);
            Assert.Equal(
                "carrier_reference_required",
                (await ReadJsonAsync(blankResponse)).GetProperty("code").GetString()
            );

            var stored = await ReadOrderAsync(orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.DispatchedAtUtc);
            Assert.Null(stored.TrackingUrl);
            Assert.Equal(0, await CountAuditAsync(AdminAuditActions.ShopDispatched, orderId.ToString("D")));
        }

        [Fact]
        public async Task Transitions_OutOfOrderOrRepeated_Return409()
        {
            var seeded = await SeedWorkspaceAsync();
            var notPrepared = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing
            );
            var prepared = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                preparationStartedAtUtc: DateTime.UtcNow.AddHours(-3)
            );
            var notProduced = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                preparationStartedAtUtc: DateTime.UtcNow.AddHours(-3)
            );

            using (var productionBeforePrep = AuthorizedPost(
                $"/api/admin/shop-orders/{notPrepared}/production-started",
                seeded.AdminJwt
            ))
            {
                var response = await _client.SendAsync(productionBeforePrep);
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
                Assert.Equal(
                    "illegal_fulfilment_transition",
                    (await ReadJsonAsync(response)).GetProperty("code").GetString()
                );
            }

            using (var repeatPrep = AuthorizedPost(
                $"/api/admin/shop-orders/{prepared}/preparation-started",
                seeded.AdminJwt
            ))
            {
                Assert.Equal(HttpStatusCode.Conflict, (await _client.SendAsync(repeatPrep)).StatusCode);
            }

            using (var dispatchBeforeProduction = AuthorizedPostJson(
                $"/api/admin/shop-orders/{notProduced}/dispatched",
                seeded.AdminJwt,
                new { carrierName = "DPD", carrierReference = "DPD-123" }
            ))
            {
                Assert.Equal(
                    HttpStatusCode.Conflict,
                    (await _client.SendAsync(dispatchBeforeProduction)).StatusCode
                );
            }

            using (var deliverBeforeDispatch = AuthorizedPost(
                $"/api/admin/shop-orders/{notProduced}/delivered",
                seeded.AdminJwt
            ))
            {
                Assert.Equal(
                    HttpStatusCode.Conflict,
                    (await _client.SendAsync(deliverBeforeDispatch)).StatusCode
                );
            }

            Assert.Equal(ShopFulfilmentStatuses.Processing, (await ReadOrderAsync(notProduced)).FulfilmentStatus);
        }

        [Fact]
        public async Task Transitions_DeniedForOperatorJwt_AndWriteNoAudit()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                preparationStartedAtUtc: DateTime.UtcNow.AddHours(-3),
                productionStartedAtUtc: DateTime.UtcNow.AddHours(-2)
            );

            var requests = new[]
            {
                AuthorizedPost($"/api/admin/shop-orders/{orderId}/preparation-started", seeded.OwnerJwt),
                AuthorizedPost($"/api/admin/shop-orders/{orderId}/production-started", seeded.OwnerJwt),
                AuthorizedPostJson(
                    $"/api/admin/shop-orders/{orderId}/dispatched",
                    seeded.OwnerJwt,
                    new { carrierName = "DPD", carrierReference = "DPD-123" }
                ),
                AuthorizedPost($"/api/admin/shop-orders/{orderId}/delivered", seeded.OwnerJwt),
            };
            foreach (var request in requests)
            {
                using (request)
                {
                    var response = await _client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                }
            }

            var stored = await ReadOrderAsync(orderId);
            Assert.Equal(ShopFulfilmentStatuses.Processing, stored.FulfilmentStatus);
            Assert.Null(stored.DispatchedAtUtc);
            Assert.Null(stored.DeliveredAtUtc);
            foreach (var action in new[]
            {
                AdminAuditActions.ShopPreparationStarted,
                AdminAuditActions.ShopProductionStarted,
                AdminAuditActions.ShopDispatched,
                AdminAuditActions.ShopDelivered,
            })
            {
                Assert.Equal(0, await CountAuditAsync(action, orderId.ToString("D")));
            }
        }

        [Fact]
        public async Task PatchFulfilment_CannotDispatchOrDeliver_UseTransitionActions()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                preparationStartedAtUtc: DateTime.UtcNow.AddHours(-3),
                productionStartedAtUtc: DateTime.UtcNow.AddHours(-2)
            );

            using var dispatch = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new { fulfilmentStatus = ShopFulfilmentStatuses.InTransit }
            );
            var response = await _client.SendAsync(dispatch);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(
                "illegal_fulfilment_transition",
                (await ReadJsonAsync(response)).GetProperty("code").GetString()
            );
            Assert.Equal(ShopFulfilmentStatuses.Processing, (await ReadOrderAsync(orderId)).FulfilmentStatus);
        }
        [Fact]
        public async Task Delivered_DoesNotMintReceiptSticker()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/stickers",
                catalogSkuId: "offer-card",
                titleSnapshot: "Offer Card"
            );

            using var deliver = AuthorizedPost(
                $"/api/admin/shop-orders/{orderId}/delivered",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(deliver)).StatusCode
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var receiptCount = await context.QrCodes.CountAsync(row =>
                row.RestaurantLocationId == seeded.LocationId
                && row.QrType == QrType.ReceiptSticker
                && row.Status == QrCodeStatus.Active
            );
            Assert.Equal(0, receiptCount);
        }
        [Fact]
        public async Task ExportCsv_UsesPersistedShipToAndLines_DefaultProcessing()
        {
            var seeded = await SeedWorkspaceAsync();
            await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                shipToContactName: "Ada Export",
                shipToAddressLine2: "Unit 4",
                deliveryInstructions: "Leave at desk"
            );
            await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/skip"
            );

            using var request = AuthorizedGet(
                "/api/admin/shop-orders/export.csv",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "text/csv",
                response.Content.Headers.ContentType?.MediaType
            );

            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("orderNumber", csv);
            Assert.Contains("locationNameSnapshot", csv);
            Assert.Contains("shipToContactName", csv);
            Assert.Contains("Ada Export", csv);
            Assert.Contains("Unit 4", csv);
            Assert.Contains("Leave at desk", csv);
            Assert.Contains("table-tents", csv);
            Assert.Contains("processing", csv);
            Assert.DoesNotContain("https://track.example/skip", csv);
        }

        [Fact]
        public async Task ExportCsv_WritesOneReadAuditRow_PerRequest()
        {
            var seeded = await SeedWorkspaceAsync();
            await InsertOrderAsync(seeded, ShopFulfilmentStatuses.Processing);
            await InsertOrderAsync(seeded, ShopFulfilmentStatuses.Processing);
            var before = await CountAuditAsync(AdminAuditActions.ShopOrdersExport, "export");

            using var request = AuthorizedGet(
                "/api/admin/shop-orders/export.csv",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                before + 1,
                await CountAuditAsync(AdminAuditActions.ShopOrdersExport, "export")
            );
        }

        private async Task<int> CountAuditAsync(string action, string targetId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            return await context.AdminAuditEvents
                .AsNoTracking()
                .CountAsync(e => e.Action == action && e.TargetId == targetId);
        }

        [Fact]
        public async Task OperatorDetail_StillOmitsOpsNotes_AfterAdminWritesNotes()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/ops"
            );

            using var patch = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new { opsNotes = "warehouse aisle B" }
            );
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(patch)).StatusCode);

            using var detail = AuthorizedGet(
                $"/api/shop/orders/{orderId}?locationId={seeded.LocationId}",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(detail);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.TryGetProperty("opsNotes", out _));
        }

        [Fact]
        public async Task MarkProductionStarted_RequiresPreparation_RepeatIsRejected_AndBlocksOperatorCancel()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                preparationStartedAtUtc: DateTime.UtcNow.AddHours(-3)
            );

            using var first = AuthorizedPost(
                $"/api/admin/shop-orders/{orderId}/production-started",
                seeded.AdminJwt
            );
            var firstResponse = await _client.SendAsync(first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            var stamped = await ReadOrderAsync(orderId);
            Assert.NotNull(stamped.ProductionStartedAtUtc);
            Assert.Equal(seeded.AdminId, stamped.ProductionStartedByAdminUserId);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var events = await context.AdminAuditEvents
                    .Where(row =>
                        row.Action == AdminAuditActions.ShopProductionStarted
                        && row.TargetId == orderId.ToString("D")
                    )
                    .ToListAsync();
                Assert.Single(events);
                Assert.Equal(AdminAuditTargetTypes.ShopOrder, events[0].TargetType);
                Assert.Equal(seeded.AdminId, events[0].ActorAdminUserId);
                Assert.Equal(seeded.RestaurantId, events[0].RestaurantId);
            }

            using var second = AuthorizedPost(
                $"/api/admin/shop-orders/{orderId}/production-started",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.Conflict,
                (await _client.SendAsync(second)).StatusCode
            );
            Assert.Equal(
                1,
                await CountAuditAsync(AdminAuditActions.ShopProductionStarted, orderId.ToString("D"))
            );

            using var detail = AuthorizedGet(
                $"/api/shop/orders/{orderId}?locationId={seeded.LocationId}",
                seeded.OwnerJwt
            );
            var detailBody = await ReadJsonAsync(await _client.SendAsync(detail));
            Assert.False(detailBody.GetProperty("canCancel").GetBoolean());
            Assert.Equal(
                "production_started",
                detailBody.GetProperty("cancelBlockReason").GetString()
            );

            using var cancel = AuthorizedPostJson(
                $"/api/shop/orders/{orderId}/cancel",
                seeded.OwnerJwt,
                new
                {
                    locationId = seeded.LocationId,
                    reason = ShopCancelReasons.OrderedByMistake,
                }
            );
            var cancelResponse = await _client.SendAsync(cancel);
            Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
            var cancelBody = await ReadJsonAsync(cancelResponse);
            Assert.Equal(
                "shop_order_not_cancellable",
                cancelBody.GetProperty("code").GetString()
            );
        }
        [Fact]
        public async Task ForceCancel_AfterStamp_Refunds_SkipRefund_AndFailClosed()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                preparationStartedAtUtc: DateTime.UtcNow.AddHours(-3)
            );
            const string revolutOrderId = "ord_admin_force_1";

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var order = await context.ShopOrders.SingleAsync(row =>
                    row.Id == orderId
                );
                order.RevolutOrderId = revolutOrderId;
                await context.SaveChangesAsync();
            }

            using var stamp = AuthorizedPost(
                $"/api/admin/shop-orders/{orderId}/production-started",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(stamp)).StatusCode
            );

            var refundBefore = _factory.Merchant.RefundOrderCallCount;
            using var forceCancel = AuthorizedPostJson(
                $"/api/admin/shop-orders/{orderId}/force-cancel",
                seeded.AdminJwt,
                new
                {
                    reason = ShopCancelReasons.NoLongerRequired,
                    skipRefund = false,
                }
            );
            var forceResponse = await _client.SendAsync(forceCancel);
            Assert.Equal(HttpStatusCode.OK, forceResponse.StatusCode);
            var forceBody = await ReadJsonAsync(forceResponse);
            Assert.Equal(
                ShopFulfilmentStatuses.Cancelled,
                forceBody.GetProperty("fulfilmentStatus").GetString()
            );
            Assert.Equal(
                refundBefore + 1,
                _factory.Merchant.RefundOrderCallCount
            );
            Assert.Equal(revolutOrderId, _factory.Merchant.LastRefundOrderId);
            Assert.Equal(
                $"shop-cancel:{orderId:D}",
                _factory.Merchant.LastRefundIdempotencyKey
            );

            var stored = await ReadOrderAsync(orderId);
            Assert.Equal(ShopFulfilmentStatuses.Cancelled, stored.FulfilmentStatus);
            Assert.Null(stored.CancelledByUserId);
            Assert.Equal(
                ShopCancelReasons.NoLongerRequired,
                stored.CancelReason
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var events = await context.AdminAuditEvents
                    .Where(row =>
                        row.Action == AdminAuditActions.ShopForceCancel
                        && row.TargetId == orderId.ToString("D")
                    )
                    .ToListAsync();
                Assert.Single(events);
                Assert.Contains("\"skipRefund\":false", events[0].DetailJson);
                Assert.Contains("\"refundAttempted\":true", events[0].DetailJson);
            }

            var skipOrderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing
            );
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var order = await context.ShopOrders.SingleAsync(row =>
                    row.Id == skipOrderId
                );
                order.RevolutOrderId = "ord_admin_force_skip";
                order.ProductionStartedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            var skipBefore = _factory.Merchant.RefundOrderCallCount;
            using var skipCancel = AuthorizedPostJson(
                $"/api/admin/shop-orders/{skipOrderId}/force-cancel",
                seeded.AdminJwt,
                new { reason = "ops free text", skipRefund = true }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(skipCancel)).StatusCode
            );
            Assert.Equal(skipBefore, _factory.Merchant.RefundOrderCallCount);

            var failOrderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing
            );
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var order = await context.ShopOrders.SingleAsync(row =>
                    row.Id == failOrderId
                );
                order.RevolutOrderId = "ord_admin_force_fail";
                order.ProductionStartedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            _factory.Merchant.NextRefundFails = true;
            using var failCancel = AuthorizedPostJson(
                $"/api/admin/shop-orders/{failOrderId}/force-cancel",
                seeded.AdminJwt,
                new
                {
                    reason = ShopCancelReasons.Other,
                    skipRefund = false,
                }
            );
            var failResponse = await _client.SendAsync(failCancel);
            Assert.Equal(HttpStatusCode.BadRequest, failResponse.StatusCode);
            var failBody = await ReadJsonAsync(failResponse);
            Assert.Equal(
                "revolut_refund_failed",
                failBody.GetProperty("code").GetString()
            );

            var failStored = await ReadOrderAsync(failOrderId);
            Assert.Equal(
                ShopFulfilmentStatuses.Processing,
                failStored.FulfilmentStatus
            );
            Assert.Null(failStored.CancelledAtUtc);
        }

        private async Task EnsureShopMaterialsAsync(Guid orderId)
        {
            using var scope = _factory.Services.CreateScope();
            await scope.ServiceProvider
                .GetRequiredService<IPrintReadyQrMaterialsService>()
                .EnsureShopOrderMaterialsAsync(orderId);
        }

        private async Task<IReadOnlyList<ShopPrintAssetReadinessDto>> ListShopReadinessAsync(
            Guid orderId
        )
        {
            using var scope = _factory.Services.CreateScope();
            return await scope.ServiceProvider
                .GetRequiredService<IPrintReadyQrMaterialsService>()
                .ListShopOrderReadinessAsync(orderId);
        }

        private async Task<ShopOrder> ReadOrderAsync(Guid orderId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            return await context.ShopOrders.AsNoTracking().SingleAsync(row =>
                row.Id == orderId
            );
        }

        private async Task<Guid> InsertOrderAsync(
            Seeded seeded,
            string? fulfilmentStatus,
            string? trackingUrl = null,
            string? opsNotes = null,
            DateTime? deliveredAtUtc = null,
            DateTime? preparationStartedAtUtc = null,
            DateTime? productionStartedAtUtc = null,
            string catalogSkuId = "table-tents",
            string titleSnapshot = "Table tents",
            string shipToContactName = "Ada Lovelace",
            string? shipToAddressLine2 = null,
            string? deliveryInstructions = null,
            int quantity = 2
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var sequence = await context.ShopOrderSequences
                .SingleOrDefaultAsync(row => row.RestaurantId == seeded.RestaurantId);
            var nextNumber = sequence?.NextNumber ?? 1;
            if (sequence == null)
            {
                context.ShopOrderSequences.Add(
                    new ShopOrderSequence
                    {
                        RestaurantId = seeded.RestaurantId,
                        NextNumber = nextNumber + 1,
                    }
                );
            }
            else
            {
                sequence.NextNumber = nextNumber + 1;
            }

            var order = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{nextNumber}",
                RestaurantId = seeded.RestaurantId,
                LocationId = seeded.LocationId,
                LocationNameSnapshot = seeded.LocationName,
                PlacedByUserId = seeded.OwnerUserId,
                PlacedByNameSnapshot = "Shop Owner",
                MaterialsNetPence = 2400,
                VatPence = 480,
                DeliveryNetPence = 0,
                GrossPence = 2880,
                DeliveryMethod = ShopDeliveryMethods.Standard,
                PaymentStatus = ShopPaymentStatuses.Paid,
                FulfilmentStatus = fulfilmentStatus,
                PaidAtUtc = DateTime.UtcNow.AddDays(-2),
                ProcessingStartedAtUtc =
                    fulfilmentStatus == ShopFulfilmentStatuses.Processing
                    || fulfilmentStatus == ShopFulfilmentStatuses.InTransit
                    || fulfilmentStatus == ShopFulfilmentStatuses.Delivered
                        ? DateTime.UtcNow.AddDays(-1)
                        : null,
                DispatchedAtUtc =
                    fulfilmentStatus == ShopFulfilmentStatuses.InTransit
                    || fulfilmentStatus == ShopFulfilmentStatuses.Delivered
                        ? DateTime.UtcNow.AddHours(-6)
                        : null,
                DeliveredAtUtc = deliveredAtUtc,
                PreparationStartedAtUtc = preparationStartedAtUtc,
                ProductionStartedAtUtc = productionStartedAtUtc,
                ProductionStartSource = productionStartedAtUtc == null
                    ? null
                    : ShopFulfilmentEvidenceSources.AdminManual,
                TrackingUrl = trackingUrl,
                OpsNotes = opsNotes,
                ShipToContactName = shipToContactName,
                ShipToContactPhone = "07700900000",
                ShipToAddressLine1 = "1 High Street",
                ShipToAddressLine2 = shipToAddressLine2,
                ShipToPostcode = "SE1 1TQ",
                ShipToCountry = "United Kingdom",
                DeliveryInstructions = deliveryInstructions,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                UpdatedAtUtc = DateTime.UtcNow,
                Lines =
                {
                    new ShopOrderLine
                    {
                        Id = Guid.NewGuid(),
                        CatalogSkuId = catalogSkuId,
                        TitleSnapshot = titleSnapshot,
                        MaterialType = titleSnapshot,
                        Quantity = quantity,
                        UnitNetPence = 1200,
                        LineNetPence = 2400,
                    },
                },
            };

            context.ShopOrders.Add(order);
            await context.SaveChangesAsync();
            return order.Id;
        }

        private async Task<Seeded> SeedWorkspaceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Admin Shop Owner",
                Email = $"owner-admin-shop-20-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
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
                Name = "Admin Shop Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main Hall",
                Address = "1 High Street",
                City = "London",
                Postcode = "SE1 1TQ",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.AddRange(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.TableTent,
                    Token = $"admin-shop-tent-{Guid.NewGuid():N}",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.WindowSticker,
                    Token = $"admin-shop-window-{Guid.NewGuid():N}",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.OfferCard,
                    Token = $"admin-shop-offer-{Guid.NewGuid():N}",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            context.BillingAccounts.Add(new BillingAccount
            {
                RestaurantId = restaurant.Id,
                BillingStatus = BillingStatuses.Active,
                ContractedPricebookId = "pricebook-v1",
            });

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            owner.SelectedRestaurantId = restaurant.Id;

            var tummlyAdmin = new Admin
            {
                FullName = "Tummly Admin",
                Email = $"admin-shop-20-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(tummlyAdmin);
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                location.Id,
                location.LocationName,
                owner.Id,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                tummlyAdmin.Id,
                jwtService.GenerateAdminToken(tummlyAdmin)
            );
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedPatch(
            string url,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedPost(
            string url,
            string jwt
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedPostJson(
            string url,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record Seeded(
            int RestaurantId,
            int LocationId,
            string LocationName,
            int OwnerUserId,
            string OwnerJwt,
            int AdminId,
            string AdminJwt
        );
    }
}
