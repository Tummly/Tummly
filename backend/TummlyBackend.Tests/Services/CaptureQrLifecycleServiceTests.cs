using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class CaptureQrLifecycleServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptureQrLifecycleService _service;
        private readonly SmartGuestLinkService _smartGuestLink;
        private int _userId;
        private int _locationId;

        public CaptureQrLifecycleServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://tummly.example",
                    }
                )
                .Build();

            _smartGuestLink = new SmartGuestLinkService(_context, configuration, new NoOpBillingAccountLifecycle());
            _service = new CaptureQrLifecycleService(
                _context,
                _smartGuestLink,
                PricebookCatalog.LoadFromDirectory(PackDirectory())
            );

            SeedWorkspace();
        }

        [Fact]
        public async Task Pause_ActiveCode_SetsPaused()
        {
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Active);

            var result = await _service.PauseAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            AssertPayload(result, "status", "Paused");
            Assert.Equal(
                QrCodeStatus.Paused,
                (await ReloadAsync(qr.Id)).Status
            );
        }

        [Fact]
        public async Task Resume_PausedCode_SetsActive()
        {
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Paused);

            var result = await _service.ResumeAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            AssertPayload(result, "status", "Active");
            Assert.Equal(
                QrCodeStatus.Active,
                (await ReloadAsync(qr.Id)).Status
            );
        }

        [Fact]
        public async Task Pause_WhenAlreadyPaused_InvalidTransition()
        {
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Paused);

            var result = await _service.PauseAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.InvalidTransition, result.Kind);
            Assert.Equal("Only Active QR codes can be paused.", result.Message);
        }

        [Fact]
        public async Task Resume_WhenActive_InvalidTransition()
        {
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Active);

            var result = await _service.ResumeAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.InvalidTransition, result.Kind);
            Assert.Equal(
                "Only Paused QR codes can be resumed.",
                result.Message
            );
        }

        [Fact]
        public async Task Pause_WhenLocationPaused_LocationLocked()
        {
            await SetLocationStatusAsync(CaptureLocationStatus.Paused);
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Paused);

            var result = await _service.PauseAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.LocationLocked, result.Kind);
            Assert.Equal(
                "Per-code Pause and Activate are unavailable while location capture is paused.",
                result.Message
            );
        }

        [Fact]
        public async Task Resume_WhenLocationPaused_LocationLocked()
        {
            await SetLocationStatusAsync(CaptureLocationStatus.Paused);
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Paused);

            var result = await _service.ResumeAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.LocationLocked, result.Kind);
        }

        [Fact]
        public async Task Rotate_ActiveCatalog_RemintsTokenAndReturnsUrl()
        {
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Active);
            var oldToken = qr.Token;

            var result = await _service.RotateAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            var reloaded = await ReloadAsync(qr.Id);
            Assert.NotEqual(oldToken, reloaded.Token);
            AssertPayload(
                result,
                "qrLinkUrl",
                _smartGuestLink.BuildGuestUrl(reloaded.Token)
            );
        }

        [Fact]
        public async Task Rotate_DigitalGuestLink_Rejected()
        {
            var qr = await SeedDigitalAsync("Newsletter", QrCodeStatus.Active);

            var result = await _service.RotateAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.InvalidTransition, result.Kind);
            Assert.Equal(
                "Digital guest links cannot be rotated.",
                result.Message
            );
        }

        [Fact]
        public async Task Rotate_Archived_Rejected()
        {
            var qr = await SeedQrAsync(
                QrType.CounterCard,
                QrCodeStatus.Archived
            );

            var result = await _service.RotateAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.InvalidTransition, result.Kind);
            Assert.Equal(
                "Only Active or Paused QR codes can be rotated.",
                result.Message
            );
        }

        [Fact]
        public async Task Archive_LiveCode_SetsArchivedAndStampsActor()
        {
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Active);

            var result = await _service.ArchiveAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            var reloaded = await ReloadAsync(qr.Id);
            Assert.Equal(QrCodeStatus.Archived, reloaded.Status);
            Assert.Equal(_userId, reloaded.ArchivedByUserId);
            Assert.Equal("Operator One", reloaded.ArchivedByDisplayName);
            Assert.NotNull(reloaded.ArchivedAt);
            AssertPayload(result, "archivedByDisplayName", "Operator One");
        }

        [Fact]
        public async Task Archive_RemovesCodeFromLocationRestoreSet()
        {
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Paused);
            var other = await SeedQrAsync(QrType.SmartGuest, QrCodeStatus.Paused);
            await SetRestoreSetAsync(qr.Id, other.Id);

            await _service.ArchiveAsync(CodeCommand(qr.Id));

            var location = await _context.RestaurantLocations.FirstAsync(
                l => l.Id == _locationId
            );
            Assert.Equal(
                new[] { other.Id },
                CaptureLocationPauseRestore.Parse(
                    location.CaptureLocationPauseRestoreQrCodeIdsJson
                )
            );
        }

        [Fact]
        public async Task Restore_Archived_ReturnsPausedWithUrl()
        {
            var qr = await SeedQrAsync(
                QrType.CounterCard,
                QrCodeStatus.Archived
            );

            var result = await _service.RestoreAsync(CodeCommand(qr.Id));

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            AssertPayload(result, "status", "Paused");
            AssertPayload(
                result,
                "qrLinkUrl",
                _smartGuestLink.BuildGuestUrl(qr.Token)
            );
            Assert.Equal(
                QrCodeStatus.Paused,
                (await ReloadAsync(qr.Id)).Status
            );
        }

        [Fact]
        public async Task Restore_WhenCatalogSlotOccupied_Conflicts()
        {
            await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Active);
            var archived = await SeedQrAsync(
                QrType.CounterCard,
                QrCodeStatus.Archived
            );

            var result = await _service.RestoreAsync(CodeCommand(archived.Id));

            Assert.Equal(QrLifecycleResultKind.Conflict, result.Kind);
            Assert.Equal("type_slot_occupied", result.Reason);
        }

        [Fact]
        public async Task Restore_WhenLinkNameOccupied_Conflicts()
        {
            await SeedDigitalAsync("VIP", QrCodeStatus.Active);
            var archived = await SeedDigitalAsync("VIP", QrCodeStatus.Archived);

            var result = await _service.RestoreAsync(CodeCommand(archived.Id));

            Assert.Equal(QrLifecycleResultKind.Conflict, result.Kind);
            Assert.Equal("link_name_occupied", result.Reason);
        }

        [Fact]
        public async Task CreateDigitalGuestLink_MintsActiveWithUniqueName()
        {
            var result = await _service.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    LinkName = "  Summer  Promo ",
                    Channel = "Email",
                    Status = "Active",
                }
            );

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            AssertPayload(result, "status", "Active");
            AssertPayload(result, "linkName", "Summer Promo");
            AssertPayload(result, "channel", "Email");
            AssertPayload(result, "createdByDisplayName", "Operator One");

            var stored = await _context.QrCodes.SingleAsync(q =>
                q.QrType == QrType.DigitalGuestLink
            );
            Assert.Equal("summer promo", stored.NormalizedLinkName);
            Assert.Equal(QrCodeStatus.Active, stored.Status);
        }

        [Fact]
        public async Task CreateDigitalGuestLink_WhenLocationPaused_ForcesPaused()
        {
            await SetLocationStatusAsync(CaptureLocationStatus.Paused);

            var result = await _service.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    LinkName = "Forced Pause",
                    Channel = "WhatsApp",
                    Status = "Active",
                }
            );

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            AssertPayload(result, "status", "Paused");
        }

        [Fact]
        public async Task CreateDigitalGuestLink_DuplicateName_Conflicts()
        {
            await SeedDigitalAsync("Taken", QrCodeStatus.Paused);

            var result = await _service.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    LinkName = "taken",
                    Channel = "WhatsApp",
                }
            );

            Assert.Equal(QrLifecycleResultKind.Conflict, result.Kind);
            Assert.Equal("linkName", result.Field);
        }

        [Theory]
        [InlineData(null, "linkName", "Link name is required.")]
        [InlineData("", "linkName", "Link name is required.")]
        public async Task CreateDigitalGuestLink_MissingLinkName_Validation(
            string? linkName,
            string field,
            string message
        )
        {
            var result = await _service.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    LinkName = linkName,
                    Channel = "Email",
                }
            );

            Assert.Equal(QrLifecycleResultKind.Validation, result.Kind);
            Assert.Equal(field, result.Field);
            Assert.Equal(message, result.Message);
        }

        [Fact]
        public async Task CreateDigitalGuestLink_MissingChannel_Validation()
        {
            var result = await _service.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    LinkName = "Ok",
                    Channel = null,
                }
            );

            Assert.Equal(QrLifecycleResultKind.Validation, result.Kind);
            Assert.Equal("channel", result.Field);
        }

        [Fact]
        public async Task CreateDigitalGuestLink_InvalidStatus_Validation()
        {
            var result = await _service.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    LinkName = "Ok",
                    Channel = "Email",
                    Status = "Archived",
                }
            );

            Assert.Equal(QrLifecycleResultKind.Validation, result.Kind);
            Assert.Equal("status", result.Field);
        }

        [Fact]
        public async Task UpdateInternalDescription_Active_Succeeds()
        {
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Active);

            var result = await _service.UpdateInternalDescriptionAsync(
                new UpdateInternalDescriptionCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    QrCodeId = qr.Id,
                    InternalDescription = "  Front counter  ",
                }
            );

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            AssertPayload(result, "internalDescription", "Front counter");
            AssertPayload(result, "updatedByDisplayName", "Operator One");
        }

        [Fact]
        public async Task UpdateInternalDescription_Archived_Rejected()
        {
            var qr = await SeedQrAsync(
                QrType.CounterCard,
                QrCodeStatus.Archived
            );

            var result = await _service.UpdateInternalDescriptionAsync(
                new UpdateInternalDescriptionCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    QrCodeId = qr.Id,
                    InternalDescription = "Nope",
                }
            );

            Assert.Equal(QrLifecycleResultKind.InvalidTransition, result.Kind);
        }

        [Fact]
        public async Task PauseLocationCapture_PausesActiveAndStoresRestoreSet()
        {
            var active = await SeedQrAsync(
                QrType.CounterCard,
                QrCodeStatus.Active
            );
            var alreadyPaused = await SeedQrAsync(
                QrType.SmartGuest,
                QrCodeStatus.Paused
            );

            var result = await _service.PauseLocationCaptureAsync(
                LocationCommand()
            );

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            AssertPayload(result, "status", "Paused");
            AssertPayload(result, "pausedCount", 1);
            AssertPayload(result, "pauseRestoreQrCodeCount", 1);

            Assert.Equal(
                QrCodeStatus.Paused,
                (await ReloadAsync(active.Id)).Status
            );
            Assert.Equal(
                QrCodeStatus.Paused,
                (await ReloadAsync(alreadyPaused.Id)).Status
            );

            var location = await _context.RestaurantLocations.FirstAsync(
                l => l.Id == _locationId
            );
            Assert.Equal(
                CaptureLocationStatus.Paused,
                location.CaptureLocationStatus
            );
            Assert.Equal(
                new[] { active.Id },
                CaptureLocationPauseRestore.Parse(
                    location.CaptureLocationPauseRestoreQrCodeIdsJson
                )
            );
        }

        [Fact]
        public async Task PauseLocationCapture_WhenAlreadyPaused_Rejected()
        {
            await SetLocationStatusAsync(CaptureLocationStatus.Paused);

            var result = await _service.PauseLocationCaptureAsync(
                LocationCommand()
            );

            Assert.Equal(QrLifecycleResultKind.InvalidTransition, result.Kind);
            Assert.Equal(
                "Location capture is already paused.",
                result.Message
            );
        }

        [Fact]
        public async Task ActivateLocationCapture_RestoresOnlyRememberedPaused()
        {
            var inSet = await SeedQrAsync(
                QrType.CounterCard,
                QrCodeStatus.Paused
            );
            var notInSet = await SeedQrAsync(
                QrType.SmartGuest,
                QrCodeStatus.Paused
            );
            await SetLocationStatusAsync(CaptureLocationStatus.Paused);
            await SetRestoreSetAsync(inSet.Id);

            var result = await _service.ActivateLocationCaptureAsync(
                LocationCommand()
            );

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            AssertPayload(result, "status", "Active");
            AssertPayload(result, "activatedCount", 1);
            AssertPayload(result, "pauseRestoreQrCodeCount", 0);

            Assert.Equal(
                QrCodeStatus.Active,
                (await ReloadAsync(inSet.Id)).Status
            );
            Assert.Equal(
                QrCodeStatus.Paused,
                (await ReloadAsync(notInSet.Id)).Status
            );

            var location = await _context.RestaurantLocations.FirstAsync(
                l => l.Id == _locationId
            );
            Assert.Equal(
                CaptureLocationStatus.Active,
                location.CaptureLocationStatus
            );
            Assert.Null(location.CaptureLocationPauseRestoreQrCodeIdsJson);
        }

        [Fact]
        public async Task ActivateLocationCapture_SkipsArchivedInRestoreSet()
        {
            var archived = await SeedQrAsync(
                QrType.CounterCard,
                QrCodeStatus.Archived
            );
            var paused = await SeedQrAsync(
                QrType.SmartGuest,
                QrCodeStatus.Paused
            );
            await SetLocationStatusAsync(CaptureLocationStatus.Paused);
            await SetRestoreSetAsync(archived.Id, paused.Id);

            var result = await _service.ActivateLocationCaptureAsync(
                LocationCommand()
            );

            Assert.Equal(QrLifecycleResultKind.Ok, result.Kind);
            AssertPayload(result, "activatedCount", 1);
            Assert.Equal(
                QrCodeStatus.Archived,
                (await ReloadAsync(archived.Id)).Status
            );
            Assert.Equal(
                QrCodeStatus.Active,
                (await ReloadAsync(paused.Id)).Status
            );
        }

        [Fact]
        public async Task ActivateLocationCapture_WhenNotPaused_Rejected()
        {
            var result = await _service.ActivateLocationCaptureAsync(
                LocationCommand()
            );

            Assert.Equal(QrLifecycleResultKind.InvalidTransition, result.Kind);
            Assert.Equal("Location capture is not paused.", result.Message);
        }

        [Fact]
        public async Task ActivateLocationCapture_WhenRestoreSetWouldExceedCap_IsAllOrNothing()
        {
            var restoreIds = new List<int>();
            for (var i = 0; i < 6; i++)
            {
                var qr = await SeedQrAsync(
                    (QrType)i,
                    QrCodeStatus.Active
                );
                restoreIds.Add(qr.Id);
            }

            var pause = await _service.PauseLocationCaptureAsync(
                LocationCommand()
            );
            Assert.Equal(QrLifecycleResultKind.Ok, pause.Kind);

            var result = await _service.ActivateLocationCaptureAsync(
                LocationCommand()
            );

            Assert.Equal(QrLifecycleResultKind.Conflict, result.Kind);
            Assert.Equal("active_qr_cap_reached", result.Code);
            Assert.Equal(5, result.Cap);
            Assert.Equal(0, result.Current);

            var location = await _context.RestaurantLocations.FirstAsync(
                l => l.Id == _locationId
            );
            Assert.Equal(
                CaptureLocationStatus.Paused,
                location.CaptureLocationStatus
            );
            Assert.False(
                string.IsNullOrEmpty(
                    location.CaptureLocationPauseRestoreQrCodeIdsJson
                )
            );

            foreach (var id in restoreIds)
            {
                Assert.Equal(
                    QrCodeStatus.Paused,
                    (await ReloadAsync(id)).Status
                );
            }
        }

        [Fact]
        public async Task CreateDigitalGuestLink_LinkNameTooLong_Validation()
        {
            var result = await _service.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    LinkName = new string('a', DigitalGuestLinkNaming.LinkNameMaxLength + 1),
                    Channel = "Email",
                }
            );

            Assert.Equal(QrLifecycleResultKind.Validation, result.Kind);
            Assert.Equal("linkName", result.Field);
            Assert.Equal(
                $"Link name must be at most {DigitalGuestLinkNaming.LinkNameMaxLength} characters.",
                result.Message
            );
        }

        [Fact]
        public async Task CreateDigitalGuestLink_DescriptionTooLong_Validation()
        {
            var result = await _service.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    LinkName = "Ok",
                    Channel = "Email",
                    InternalDescription = new string(
                        'x',
                        DigitalGuestLinkNaming.InternalDescriptionMaxLength + 1
                    ),
                }
            );

            Assert.Equal(QrLifecycleResultKind.Validation, result.Kind);
            Assert.Equal("internalDescription", result.Field);
        }

        [Fact]
        public async Task UpdateInternalDescription_TooLong_Validation()
        {
            var qr = await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Active);

            var result = await _service.UpdateInternalDescriptionAsync(
                new UpdateInternalDescriptionCommand
                {
                    UserId = _userId,
                    LocationId = _locationId,
                    QrCodeId = qr.Id,
                    InternalDescription = new string(
                        'x',
                        DigitalGuestLinkNaming.InternalDescriptionMaxLength + 1
                    ),
                }
            );

            Assert.Equal(QrLifecycleResultKind.Validation, result.Kind);
            Assert.Equal("internalDescription", result.Field);
        }

        [Fact]
        public async Task Pause_UnknownQr_NotFound()
        {
            var result = await _service.PauseAsync(CodeCommand(99999));

            Assert.Equal(QrLifecycleResultKind.NotFound, result.Kind);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedWorkspace()
        {
            var user = new User
            {
                FullName = "Operator One",
                Email = "op@example.com",
                PasswordHash = "x",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            _userId = user.Id;

            var restaurant = new Restaurant
            {
                Name = "Test Restaurant",
                AccountType = "Single",
                OwnerUserId = _userId,
                CreatedAt = DateTime.UtcNow,
                BillingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                    0,
                    "TUMMLY-UK-GBP-2026-08-V3"
                ),
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
                CaptureLocationStatus = CaptureLocationStatus.Active,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();
            _locationId = location.Id;
        }

        private async Task<QrCode> SeedQrAsync(
            QrType qrType,
            QrCodeStatus status
        )
        {
            var qr = new QrCode
            {
                RestaurantLocationId = _locationId,
                QrType = qrType,
                Token = Guid.NewGuid().ToString("N")[..32],
                Status = status,
                CreatedAt = DateTime.UtcNow,
                ArchivedAt = status == QrCodeStatus.Archived
                    ? DateTime.UtcNow
                    : null,
            };
            _context.QrCodes.Add(qr);
            await _context.SaveChangesAsync();
            return qr;
        }

        private async Task<QrCode> SeedDigitalAsync(
            string linkName,
            QrCodeStatus status
        )
        {
            var formatted = DigitalGuestLinkNaming.FormatLinkName(linkName);
            var qr = new QrCode
            {
                RestaurantLocationId = _locationId,
                QrType = QrType.DigitalGuestLink,
                Token = Guid.NewGuid().ToString("N")[..32],
                Status = status,
                LinkName = formatted,
                NormalizedLinkName =
                    DigitalGuestLinkNaming.NormalizeLinkName(formatted),
                Channel = DigitalGuestLinkChannel.Email,
                CreatedAt = DateTime.UtcNow,
                ArchivedAt = status == QrCodeStatus.Archived
                    ? DateTime.UtcNow
                    : null,
            };
            _context.QrCodes.Add(qr);
            await _context.SaveChangesAsync();
            return qr;
        }

        private async Task SetLocationStatusAsync(CaptureLocationStatus status)
        {
            var location = await _context.RestaurantLocations.FirstAsync(
                l => l.Id == _locationId
            );
            location.CaptureLocationStatus = status;
            await _context.SaveChangesAsync();
        }

        private async Task SetRestoreSetAsync(params int[] ids)
        {
            var location = await _context.RestaurantLocations.FirstAsync(
                l => l.Id == _locationId
            );
            location.CaptureLocationPauseRestoreQrCodeIdsJson =
                CaptureLocationPauseRestore.Serialize(ids);
            await _context.SaveChangesAsync();
        }

        private QrCodeLifecycleCommand CodeCommand(int qrCodeId) =>
            new()
            {
                UserId = _userId,
                LocationId = _locationId,
                QrCodeId = qrCodeId,
            };

        private LocationCaptureLifecycleCommand LocationCommand() =>
            new() { UserId = _userId, LocationId = _locationId };

        private async Task<QrCode> ReloadAsync(int qrCodeId) =>
            await _context.QrCodes
                .AsNoTracking()
                .FirstAsync(q => q.Id == qrCodeId);

        private static string PackDirectory()
        {
            var dir = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "docs",
                    "product",
                    "billing-pack-v3.0"
                )
            );
            if (!Directory.Exists(dir))
            {
                dir = Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "docs",
                        "product",
                        "billing-pack-v3.0"
                    )
                );
            }

            return dir;
        }

        private static void AssertPayload(
            QrLifecycleResult result,
            string property,
            object? expected
        )
        {
            Assert.NotNull(result.Payload);
            var json = JsonSerializer.Serialize(result.Payload);
            using var doc = JsonDocument.Parse(json);
            Assert.True(
                doc.RootElement.TryGetProperty(property, out var value),
                $"Missing payload property '{property}'"
            );

            switch (expected)
            {
                case string s:
                    Assert.Equal(s, value.GetString());
                    break;
                case int i:
                    Assert.Equal(i, value.GetInt32());
                    break;
                case null:
                    Assert.Equal(JsonValueKind.Null, value.ValueKind);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported expected type {expected.GetType()}"
                    );
            }
        }
    }
}
