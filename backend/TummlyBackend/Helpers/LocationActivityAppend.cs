using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class LocationActivityAppend
    {
        public static void AppendRestaurantActivity(
            ApplicationDbContext context,
            int restaurantId,
            int actorUserId,
            string actorDisplayName,
            string kind,
            string description,
            DateTime occurredAt
        )
        {
            context.LocationActivities.Add(
                new LocationActivity
                {
                    RestaurantId = restaurantId,
                    LocationId = null,
                    ActorUserId = actorUserId,
                    ActorDisplayName = actorDisplayName,
                    Kind = kind,
                    Description = description,
                    OccurredAt = occurredAt,
                }
            );
        }
    }
}
