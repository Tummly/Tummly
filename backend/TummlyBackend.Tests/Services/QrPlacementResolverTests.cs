using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class QrPlacementResolverTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly QrPlacementResolver _resolver;
        private readonly DateTime _baseTime = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        private int _locationId;

        public QrPlacementResolverTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _resolver = new QrPlacementResolver(_context);
            _locationId = 1;
        }

        [Fact]
        public async Task Resolve_ActiveExists_ReturnsOldestReuse()
        {
            var newer = await SeedQrAsync(QrType.GuestCard, QrCodeStatus.Active, _baseTime.AddDays(2));
            var oldest = await SeedQrAsync(QrType.GuestCard, QrCodeStatus.Active, _baseTime);
            await SeedQrAsync(QrType.GuestCard, QrCodeStatus.Paused, _baseTime.AddDays(-5));

            var result = await _resolver.ResolveAsync(_locationId, QrType.GuestCard, CancellationToken.None);

            Assert.Equal(new Reuse(oldest.Id), result);
            Assert.NotEqual(newer.Id, oldest.Id);
        }

        [Fact]
        public async Task Resolve_OnlyPaused_ReturnsNeedsSlotWithOldestPausedId()
        {
            var newerPaused = await SeedQrAsync(QrType.TableTent, QrCodeStatus.Paused, _baseTime.AddDays(3));
            var olderPaused = await SeedQrAsync(QrType.TableTent, QrCodeStatus.Paused, _baseTime);

            var result = await _resolver.ResolveAsync(_locationId, QrType.TableTent, CancellationToken.None);

            Assert.Equal(new NeedsSlot(olderPaused.Id), result);
            Assert.NotEqual(newerPaused.Id, olderPaused.Id);
        }

        [Fact]
        public async Task Resolve_OnlyArchived_ReturnsNeedsSlotNull()
        {
            var archived = await SeedQrAsync(QrType.TableTalker, QrCodeStatus.Archived, _baseTime);
            archived.ArchivedAt = _baseTime.AddDays(1);
            // Paused but archived: the ArchivedAt guard alone must exclude it.
            var pausedArchived = await SeedQrAsync(QrType.TableTalker, QrCodeStatus.Paused, _baseTime.AddDays(-1));
            pausedArchived.ArchivedAt = _baseTime.AddDays(1);
            await _context.SaveChangesAsync();

            var result = await _resolver.ResolveAsync(_locationId, QrType.TableTalker, CancellationToken.None);

            Assert.Equal(new NeedsSlot(null), result);
        }

        [Fact]
        public async Task Resolve_None_ReturnsNeedsSlotNull()
        {
            var result = await _resolver.ResolveAsync(_locationId, QrType.WindowSticker, CancellationToken.None);

            Assert.Equal(new NeedsSlot(null), result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<QrCode> SeedQrAsync(QrType qrType, QrCodeStatus status, DateTime createdAt)
        {
            var qr = new QrCode
            {
                RestaurantLocationId = _locationId,
                QrType = qrType,
                Token = Guid.NewGuid().ToString("N"),
                Status = status,
                CreatedAt = createdAt,
            };
            _context.QrCodes.Add(qr);
            await _context.SaveChangesAsync();
            return qr;
        }
    }
}
