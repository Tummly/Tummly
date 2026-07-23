using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestNotesService : IGuestNotesService
    {
        public const int DefaultListLimit = 50;
        public const int MaxListLimit = 100;
        public const int MaxBodyLength = 5000;

        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestActivityEmitter _activity;

        public GuestNotesService(
            ApplicationDbContext context,
            ILocationGuestActivityEmitter activity
        )
        {
            _context = context;
            _activity = activity;
        }

        public async Task<GuestNotesListResponse?> ListAsync(
            int locationGuestId,
            int locationId,
            int limit,
            CancellationToken cancellationToken = default
        )
        {
            var exists = await _context.LocationGuests
                .AsNoTracking()
                .AnyAsync(
                    lg =>
                        lg.Id == locationGuestId
                        && lg.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (!exists)
            {
                return null;
            }

            var clampedLimit = ClampLimit(limit);

            var query = _context.LocationGuestNotes
                .AsNoTracking()
                .Where(n => n.LocationGuestId == locationGuestId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(clampedLimit)
                .Select(n => new GuestNoteItemDto
                {
                    Id = n.Id,
                    Body = n.Body,
                    AuthorDisplayName = n.AuthorDisplayName,
                    CreatedAt = n.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return new GuestNotesListResponse
            {
                Items = items,
                TotalCount = totalCount,
            };
        }

        public async Task<GuestNoteItemDto?> CreateAsync(
            int locationGuestId,
            int locationId,
            int authorUserId,
            string authorDisplayName,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            var trimmed = (body ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Note body is required.");
            }

            if (trimmed.Length > MaxBodyLength)
            {
                throw new ArgumentException(
                    $"Note body must be at most {MaxBodyLength} characters."
                );
            }

            var exists = await _context.LocationGuests
                .AsNoTracking()
                .AnyAsync(
                    lg =>
                        lg.Id == locationGuestId
                        && lg.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (!exists)
            {
                return null;
            }

            var createdAt = DateTime.UtcNow;
            var note = new LocationGuestNote
            {
                LocationGuestId = locationGuestId,
                Body = trimmed,
                AuthorUserId = authorUserId,
                AuthorDisplayName = authorDisplayName,
                CreatedAt = createdAt,
            };

            _context.LocationGuestNotes.Add(note);
            _activity.EmitNoteAdded(
                locationGuestId,
                authorDisplayName,
                createdAt
            );
            await _context.SaveChangesAsync(cancellationToken);

            return new GuestNoteItemDto
            {
                Id = note.Id,
                Body = note.Body,
                AuthorDisplayName = note.AuthorDisplayName,
                CreatedAt = note.CreatedAt,
            };
        }

        public static int ClampLimit(int limit)
        {
            if (limit < 1)
            {
                return DefaultListLimit;
            }

            return Math.Min(limit, MaxListLimit);
        }
    }
}
