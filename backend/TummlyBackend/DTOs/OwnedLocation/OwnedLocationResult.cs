using TummlyBackend.Models;

namespace TummlyBackend.DTOs.OwnedLocation
{
    public class OwnedLocationResult
    {
        public OwnedLocationResolveStatus Status { get; init; }

        public RestaurantLocation? Location { get; init; }
    }
}
