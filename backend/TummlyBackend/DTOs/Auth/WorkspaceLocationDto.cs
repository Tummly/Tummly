namespace TummlyBackend.DTOs.Auth
{
    public class WorkspaceLocationDto
    {
        public int LocationId { get; set; }

        public string LocationName { get; set; } = string.Empty;

        public string RestaurantName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}
