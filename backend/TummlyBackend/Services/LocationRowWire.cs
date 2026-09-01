using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    internal static class LocationRowWire
    {
        internal static string ToLifecycleWire(LocationLifecycleStatus status) =>
            status switch
            {
                LocationLifecycleStatus.Draft => "draft",
                LocationLifecycleStatus.Active => "active",
                LocationLifecycleStatus.Paused => "paused",
                LocationLifecycleStatus.Archived => "archived",
                _ => "active",
            };

        internal static string? NormalizeCity(string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            return city.Trim();
        }
    }
}
