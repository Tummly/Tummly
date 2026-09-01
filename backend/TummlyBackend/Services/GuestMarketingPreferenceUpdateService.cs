using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestMarketingPreferenceUpdateService
        : IGuestMarketingPreferenceUpdateService
    {
        private const string NoteSaveFailedMessage = "Could not save the note.";

        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestActivityRecorder _recorder;
        private readonly IGuestNotesService _notes;
        private readonly ILocationGuestPermissionLedgerService _permissions;

        public GuestMarketingPreferenceUpdateService(
            ApplicationDbContext context,
            ILocationGuestActivityRecorder recorder,
            IGuestNotesService notes,
            ILocationGuestPermissionLedgerService? permissions = null
        )
        {
            _context = context;
            _recorder = recorder;
            _notes = notes;
            _permissions =
                permissions ?? new LocationGuestPermissionLedgerService(context);
        }

        public async Task<GuestMarketingPreferenceUpdateOutcome> UpdateAsync(
            int locationGuestId,
            int locationId,
            int actorUserId,
            PatchGuestMarketingPreferenceRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(request.Preference))
            {
                return GuestMarketingPreferenceUpdateOutcome.ValidationError(
                    "Location Guest marketing preference is required."
                );
            }

            if (!LocationGuestMarketingPreferenceExtensions.TryFromWireString(
                    request.Preference,
                    out var next
                ))
            {
                return GuestMarketingPreferenceUpdateOutcome.ValidationError(
                    "Unknown Location Guest marketing preference."
                );
            }

            var noteBody = (request.Note ?? string.Empty).Trim();
            if (noteBody.Length > GuestNotesService.MaxBodyLength)
            {
                return GuestMarketingPreferenceUpdateOutcome.ValidationError(
                    $"Note body must be at most {GuestNotesService.MaxBodyLength} characters."
                );
            }

            var locationGuest = await _context.LocationGuests
                .FirstOrDefaultAsync(
                    lg =>
                        lg.Id == locationGuestId
                        && lg.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (locationGuest == null)
            {
                return GuestMarketingPreferenceUpdateOutcome.NotFound();
            }

            var current = locationGuest.MarketingPreference;
            if (!current.OperatorMayTransitionTo(next))
            {
                return GuestMarketingPreferenceUpdateOutcome.ValidationError(
                    "Cannot set marketing preference to allowed unless it is already allowed."
                );
            }

            var preferenceChanged = current != next;
            if (preferenceChanged)
            {
                var actor = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.Id == actorUserId,
                        cancellationToken
                    );

                if (actor == null)
                {
                    throw new InvalidOperationException("User not found.");
                }

                locationGuest.MarketingPreference = next;
                _recorder.RecordMarketingPreferenceChanged(
                    locationGuest.Id,
                    current.ToWireString(),
                    next.ToWireString(),
                    actor.FullName,
                    DateTime.UtcNow
                );

                var changedAt = DateTime.UtcNow;
                foreach (
                    var (kind, eventKind) in LocationGuestChannelPermissionGate
                        .LedgerEventsForOperatorMarketingPreference(next)
                )
                {
                    _permissions.RecordEvent(
                        locationGuest.Id,
                        locationId,
                        kind,
                        eventKind,
                        LocationGuestPermissionLedgerSources.Operator,
                        changedAt,
                        actorUserId
                    );
                }

                await _permissions.SyncMarketingPreferenceRollupAsync(
                    locationGuest,
                    cancellationToken
                );

                if (next == LocationGuestMarketingPreference.OptedOut)
                {
                    var restaurantId = await _context.RestaurantLocations
                        .AsNoTracking()
                        .Where(row => row.Id == locationId)
                        .Select(row => row.RestaurantId)
                        .FirstAsync(cancellationToken);

                    _context.LocationActivities.Add(
                        new LocationActivity
                        {
                            RestaurantId = restaurantId,
                            LocationId = locationId,
                            ActorUserId = actorUserId,
                            ActorDisplayName = string.IsNullOrWhiteSpace(
                                actor.FullName
                            )
                                ? null
                                : actor.FullName.Trim(),
                            Kind =
                                LocationActivityKinds.GuestMarketingUnsubscribed,
                            Description =
                                "Marketing preference set to opted out at this location.",
                            OccurredAt = DateTime.UtcNow,
                        }
                    );
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            var noteCreated = false;
            string? noteError = null;
            if (noteBody.Length > 0)
            {
                try
                {
                    var note = await _notes.CreateAsync(
                        locationGuestId,
                        locationId,
                        actorUserId,
                        noteBody,
                        cancellationToken
                    );

                    if (note == null)
                    {
                        noteError = NoteSaveFailedMessage;
                    }
                    else
                    {
                        noteCreated = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    noteError = NoteSaveFailedMessage;
                }
            }

            return GuestMarketingPreferenceUpdateOutcome.Updated(
                new PatchGuestMarketingPreferenceResult
                {
                    Success = true,
                    Preference = next.ToWireString(),
                    PreferenceChanged = preferenceChanged,
                    NoteCreated = noteCreated,
                    NoteError = noteError,
                }
            );
        }
    }
}
