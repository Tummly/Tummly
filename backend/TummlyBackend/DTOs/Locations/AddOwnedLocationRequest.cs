namespace TummlyBackend.DTOs.Locations
{
    public sealed class AddOwnedLocationRequest
    {
        public string LocationName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string? Postcode { get; set; }

        public string? LocationPhone { get; set; }

        public string? LocalContact { get; set; }

        /// <summary>
        /// Optional FSA (Food Standards Agency) establishment id. Stored as
        /// typed; no external FSA lookup.
        /// </summary>
        public string? FsaEstablishmentId { get; set; }
    }
}
