namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminAuditEventDto
    {
        public Guid Id { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public string Action { get; set; } = string.Empty;

        public int? ActorAdminUserId { get; set; }

        public string ActorIdentity { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public string TargetId { get; set; } = string.Empty;

        public int? RestaurantId { get; set; }

        public string? DetailJson { get; set; }

        public bool Succeeded { get; set; }
    }
}
