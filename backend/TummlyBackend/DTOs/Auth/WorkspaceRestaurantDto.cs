namespace TummlyBackend.DTOs.Auth
{
    public class WorkspaceRestaurantDto
    {
        public int RestaurantId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        /// <summary>
        /// Same as RestaurantId. Keeps the existing sign-in picker contract.
        /// </summary>
        public int LocationId { get; set; }

        public string LocationName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}
