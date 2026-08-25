namespace TummlyBackend.DTOs.Auth
{
    public class SelectWorkspaceDto
    {
        public int RestaurantId { get; set; }

        public int LocationId { get; set; }

        public int ResolveRestaurantId()
        {
            return RestaurantId > 0 ? RestaurantId : LocationId;
        }
    }
}
