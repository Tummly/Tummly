namespace TummlyBackend.DTOs.HelpCentre
{
    public class CreateHelpCentreQueryDto
    {
        /// <summary>
        /// Required for contact queries. Account requests derive topic server-side.
        /// Nullable so empty form values do not trip ASP.NET implicit [Required].
        /// </summary>
        public string? Topic { get; set; }

        public string BusinessName { get; set; } = string.Empty;

        public string SubmitterName { get; set; } = string.Empty;

        public string SubmitterEmail { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public int? RestaurantLocationId { get; set; }

        public string? AccountRequestKind { get; set; }

        public int? RestaurantId { get; set; }

        /// <summary>
        /// Required for contact queries. Account requests derive message server-side.
        /// </summary>
        public string? Message { get; set; }
    }
}
