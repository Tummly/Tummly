using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models;

// Definitions are immutable. Campaigns keep a stable reference when an audience is reused.
public sealed class CampaignAudience
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int RestaurantLocationId { get; set; }
    [MaxLength(150)] public string? Name { get; set; }
    public string DefinitionJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed record CampaignAudienceDefinition
{
    public string BaseAudienceKey { get; init; } = "all-eligible-guests";
    public int[] GuestIds { get; init; } = [];
    public int[] ExcludedGuestIds { get; init; } = [];
    public string[] GuestNames { get; init; } = [];
    public string[] ExcludedNames { get; init; } = [];
    public int? Limit { get; init; }
    public string Order { get; init; } = "newest";
    public DateTime? JoinedFromUtc { get; init; }
    public DateTime? JoinedToUtc { get; init; }
    public string? NameContains { get; init; }
}
