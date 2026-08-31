using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Settings location lifecycle writes with Capture cascade on Pause/Resume.
    /// </summary>
    public sealed class LocationLifecycleService : ILocationLifecycleService
    {
        private static readonly JsonSerializerOptions ParamsJsonOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly ApplicationDbContext _context;
        private readonly ICaptureQrLifecycleService _captureLifecycle;

        public LocationLifecycleService(
            ApplicationDbContext context,
            ICaptureQrLifecycleService captureLifecycle
        )
        {
            _context = context;
            _captureLifecycle = captureLifecycle;
        }

        public async Task<LocationLifecycleResult> PauseAsync(
            LocationLifecycleCommand command
        )
        {
            var location = await LoadOwnedLocationAsync(command);
            if (location == null)
            {
                return LocationLifecycleResult.NotFound();
            }

            if (location.LifecycleStatus != LocationLifecycleStatus.Active)
            {
                return LocationLifecycleResult.InvalidTransition(
                    "Only an Active location can be paused."
                );
            }

            if (location.CaptureLocationStatus != CaptureLocationStatus.Paused)
            {
                var capture = await _captureLifecycle.PauseLocationCaptureAsync(
                    new LocationCaptureLifecycleCommand
                    {
                        UserId = command.UserId,
                        LocationId = command.LocationId,
                    }
                );
                // Already Paused → skip; do not fail Settings Pause.
                if (
                    capture.Kind != QrLifecycleResultKind.Ok
                    && capture.Kind != QrLifecycleResultKind.InvalidTransition
                )
                {
                    return LocationLifecycleResult.InvalidTransition(
                        capture.Message
                            ?? "Could not pause location capture."
                    );
                }

                await _context.Entry(location).ReloadAsync();
            }

            var from = LocationLifecycleStatus.Active;
            location.LifecycleStatus = LocationLifecycleStatus.Paused;

            await EmitLifecycleChangedAsync(
                command,
                location,
                from,
                LocationLifecycleStatus.Paused,
                description: "Location paused"
            );
            await _context.SaveChangesAsync();

            return LocationLifecycleResult.Ok("paused");
        }

        public async Task<LocationLifecycleResult> ResumeAsync(
            LocationLifecycleCommand command
        )
        {
            var location = await LoadOwnedLocationAsync(command);
            if (location == null)
            {
                return LocationLifecycleResult.NotFound();
            }

            if (location.LifecycleStatus != LocationLifecycleStatus.Paused)
            {
                return LocationLifecycleResult.InvalidTransition(
                    "Only a Paused location can be resumed."
                );
            }

            if (location.CaptureLocationStatus == CaptureLocationStatus.Paused)
            {
                var capture =
                    await _captureLifecycle.ActivateLocationCaptureAsync(
                        new LocationCaptureLifecycleCommand
                        {
                            UserId = command.UserId,
                            LocationId = command.LocationId,
                        }
                    );
                if (
                    capture.Kind != QrLifecycleResultKind.Ok
                    && capture.Kind != QrLifecycleResultKind.InvalidTransition
                )
                {
                    return LocationLifecycleResult.InvalidTransition(
                        capture.Message
                            ?? "Could not activate location capture."
                    );
                }

                await _context.Entry(location).ReloadAsync();
            }

            var from = LocationLifecycleStatus.Paused;
            location.LifecycleStatus = LocationLifecycleStatus.Active;

            await EmitLifecycleChangedAsync(
                command,
                location,
                from,
                LocationLifecycleStatus.Active,
                description: "Location resumed"
            );
            await _context.SaveChangesAsync();

            return LocationLifecycleResult.Ok("active");
        }

        public async Task<LocationLifecycleResult> ArchiveAsync(
            LocationLifecycleCommand command
        )
        {
            var location = await LoadOwnedLocationAsync(command);
            if (location == null)
            {
                return LocationLifecycleResult.NotFound();
            }

            if (location.LifecycleStatus != LocationLifecycleStatus.Paused)
            {
                return LocationLifecycleResult.InvalidTransition(
                    "Archive is only allowed from Paused. Pause the location first."
                );
            }

            var from = location.LifecycleStatus;
            location.LifecycleStatus = LocationLifecycleStatus.Archived;

            await EmitLifecycleChangedAsync(
                command,
                location,
                from,
                LocationLifecycleStatus.Archived,
                description: "Location archived"
            );
            await _context.SaveChangesAsync();

            return LocationLifecycleResult.Ok("archived");
        }

        public async Task<LocationLifecycleResult> RestoreAsync(
            LocationLifecycleCommand command
        )
        {
            var location = await LoadOwnedLocationAsync(command);
            if (location == null)
            {
                return LocationLifecycleResult.NotFound();
            }

            if (location.LifecycleStatus != LocationLifecycleStatus.Archived)
            {
                return LocationLifecycleResult.InvalidTransition(
                    "Only an Archived location can be restored."
                );
            }

            var from = location.LifecycleStatus;
            location.LifecycleStatus = LocationLifecycleStatus.Paused;

            await EmitLifecycleChangedAsync(
                command,
                location,
                from,
                LocationLifecycleStatus.Paused,
                description: "Location restored"
            );
            await _context.SaveChangesAsync();

            return LocationLifecycleResult.Ok("paused");
        }

        private async Task<RestaurantLocation?> LoadOwnedLocationAsync(
            LocationLifecycleCommand command
        )
        {
            return await _context.RestaurantLocations.FirstOrDefaultAsync(l =>
                l.Id == command.LocationId
                && l.RestaurantId == command.RestaurantId
            );
        }

        private async Task EmitLifecycleChangedAsync(
            LocationLifecycleCommand command,
            RestaurantLocation location,
            LocationLifecycleStatus from,
            LocationLifecycleStatus to,
            string description
        )
        {
            var actorDisplayName = await _context
                .Users.AsNoTracking()
                .Where(u => u.Id == command.UserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            _context.LocationSettingsActivityEvents.Add(
                new LocationSettingsActivityEvent
                {
                    RestaurantId = command.RestaurantId,
                    LocationId = location.Id,
                    ActorUserId = command.UserId,
                    ActorDisplayName = actorDisplayName,
                    Kind = LocationSettingsActivityKinds.LifecycleChanged,
                    Description = description,
                    ParamsJson = JsonSerializer.Serialize(
                        new
                        {
                            from = ToWire(from),
                            to = ToWire(to),
                        },
                        ParamsJsonOptions
                    ),
                    OccurredAt = DateTime.UtcNow,
                }
            );
        }

        private static string ToWire(LocationLifecycleStatus status) =>
            status switch
            {
                LocationLifecycleStatus.Draft => "draft",
                LocationLifecycleStatus.Active => "active",
                LocationLifecycleStatus.Paused => "paused",
                LocationLifecycleStatus.Archived => "archived",
                _ => status.ToString().ToLowerInvariant(),
            };
    }
}
