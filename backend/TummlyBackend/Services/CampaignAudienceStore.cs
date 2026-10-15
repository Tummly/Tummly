using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Services;

public static class CampaignAudienceStore
{
    public static async Task<string> DescribeAsync(ApplicationDbContext db, int locationId, string key, CancellationToken ct)
    {
        var rule = Load(db, locationId, key);
        var ids = rule.GuestIds.ToList();
        var excludedIds = rule.ExcludedGuestIds.ToList();
        var names = await db.LocationGuests.Where(g => g.RestaurantLocationId == locationId && ids.Contains(g.Id))
            .OrderBy(g => g.Name).Select(g => g.Name).ToListAsync(ct);
        var excludedNames = await db.LocationGuests.Where(g => g.RestaurantLocationId == locationId && excludedIds.Contains(g.Id))
            .OrderBy(g => g.Name).Select(g => g.Name).ToListAsync(ct);
        var label = names.Count > 0 ? string.Join(", ", names) + " only"
            : AssistantCampaignDraftBind.AudienceLabels.GetValueOrDefault(rule.BaseAudienceKey, "Custom audience");
        if (excludedNames.Count > 0) label += " · excluding " + string.Join(", ", excludedNames);
        if (rule.Limit.HasValue) label += $" · up to {rule.Limit} ({rule.Order})";
        if (rule.JoinedFromUtc.HasValue) label += $" · joined from {rule.JoinedFromUtc:dd MMM yyyy}";
        if (rule.JoinedToUtc.HasValue) label += $" · joined before {rule.JoinedToUtc:dd MMM yyyy}";
        if (!string.IsNullOrWhiteSpace(rule.NameContains)) label += $" · name contains {rule.NameContains}";
        return label;
    }

    public static async Task<AssistantCampaignDraftBindFields> BindAsync(ApplicationDbContext db,
        int locationId, AssistantCampaignDraftBindFields fields, CancellationToken ct)
    {
        var label = fields.AudienceLabel;
        var request = fields.AudienceRequest ?? "";
        var savedMatch = System.Text.RegularExpressions.Regex.Match(request,
            "(?:use |using |for )saved audience [\"']?(?<name>[^\"'\r\n]+?)[\"']?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (savedMatch.Success)
        {
            var name = savedMatch.Groups["name"].Value.Trim();
            var saved = await db.CampaignAudiences.Where(a => a.RestaurantLocationId == locationId && a.Name == name).ToListAsync(ct);
            if (saved.Count != 1) throw new ArgumentException("Select one saved audience at this location.");
            return fields with { AudienceKey = $"audience:{saved[0].Id:N}", AudienceLabel = name };
        }
        var names = label.StartsWith("Named guests: ", StringComparison.Ordinal)
            ? label[14..].Split(", ", StringSplitOptions.RemoveEmptyEntries) : [];
        var exclusions = System.Text.RegularExpressions.Regex.Match(fields.LimitNote ?? "",
            @"Excluding: (?<names>.+?)\.$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var excluded = exclusions.Success ? exclusions.Groups["names"].Value.Split(", ") : [];
        if (AssistantCampaignDraftBind.TryExtractExclusions(request, out var requestExclusions))
            excluded = requestExclusions.ToArray();
        var joined = System.Text.RegularExpressions.Regex.Match(request, @"joined (?:in )?(?:the )?last (?<days>\d+) days?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        DateTime? joinedFrom = null;
        if (joined.Success)
        {
            if (!int.TryParse(joined.Groups["days"].Value, out var days) || days < 1 || days > 36500)
                throw new ArgumentException("Select a date range between 1 and 36500 days.");
            joinedFrom = DateTime.UtcNow.AddDays(-days);
        }
        var cap = AssistantCampaignDraftBind.TryReadGuestCap(label + " " + fields.LimitNote);
        if (names.Length == 0 && excluded.Length == 0 && cap == null && joinedFrom == null) return fields;
        var key = await CreateAsync(db, locationId, new CampaignAudienceDefinition {
            BaseAudienceKey = fields.AudienceKey,
            GuestNames = names, ExcludedNames = excluded, Limit = cap,
            JoinedFromUtc = joinedFrom,
            Order = label.Contains("newest", StringComparison.OrdinalIgnoreCase) ? "newest" : "most-recent"
        }, null, ct);
        return fields with { AudienceKey = key };
    }

    public static bool IsKey(string key) => key.StartsWith("audience:", StringComparison.Ordinal)
        && Guid.TryParseExact(key[9..], "N", out _);

    public static void Validate(CampaignAudienceDefinition definition)
    {
        if (definition is null || definition.GuestIds is null || definition.ExcludedGuestIds is null
            || definition.GuestNames is null || definition.ExcludedNames is null
            || !CampaignProductAllowLists.AudienceKeys.Contains(definition.BaseAudienceKey)
            || definition.Limit is < 1 or > 10000
            || definition.Order is not ("newest" or "most-recent")
            || definition.GuestIds.Length > 1000 || definition.ExcludedGuestIds.Length > 1000
            || definition.GuestNames.Length > 100 || definition.ExcludedNames.Length > 100
            || definition.GuestIds.Concat(definition.ExcludedGuestIds).Any(id => id < 1)
            || definition.GuestNames.Concat(definition.ExcludedNames).Any(string.IsNullOrWhiteSpace)
            || definition.JoinedFromUtc >= definition.JoinedToUtc)
            throw new ArgumentException("Invalid audience selection rules.");
    }

    public static CampaignAudienceDefinition Load(ApplicationDbContext db, int locationId, string key)
    {
        if (!IsKey(key)) throw new ArgumentException("Invalid audience key.");
        var id = Guid.ParseExact(key[9..], "N");
        var row = db.Set<CampaignAudience>().AsNoTracking()
            .SingleOrDefault(a => a.Id == id && a.RestaurantLocationId == locationId)
            ?? throw new ArgumentException("Audience not found at this location.");
        return JsonSerializer.Deserialize<CampaignAudienceDefinition>(row.DefinitionJson)
            ?? throw new ArgumentException("Invalid stored audience.");
    }

    public static async Task<string> CreateAsync(ApplicationDbContext db, int locationId,
        CampaignAudienceDefinition definition, string? name, CancellationToken ct)
    {
        Validate(definition);
        if (name is not null && (string.IsNullOrWhiteSpace(name) || name.Length > 150))
            throw new ArgumentException("Audience name must contain 1 to 150 characters.");
        var ids = definition.GuestIds.Concat(definition.ExcludedGuestIds).Distinct().ToList();
        if (await db.LocationGuests.CountAsync(g => g.RestaurantLocationId == locationId && ids.Contains(g.Id), ct) != ids.Count)
            throw new ArgumentException("Guest selection contains a guest from another location.");
        // Resolve names now. Missing and ambiguous names must never broaden the audience.
        async Task<int[]> Resolve(string[] names)
        {
            var resolved = new List<int>();
            foreach (var nameValue in names)
            {
                var normalized = nameValue.Trim().ToUpperInvariant();
                var matches = await db.LocationGuests.Where(g => g.RestaurantLocationId == locationId
                    && g.Name.ToUpper() == normalized).Select(g => g.Id).ToListAsync(ct);
                if (matches.Count != 1)
                    throw new ArgumentException($"Guest '{nameValue}' has {matches.Count} matches. Select a specific guest.");
                resolved.Add(matches[0]);
            }
            return resolved.ToArray();
        }
        definition = definition with {
            GuestIds = definition.GuestIds.Concat(await Resolve(definition.GuestNames)).Distinct().ToArray(),
            ExcludedGuestIds = definition.ExcludedGuestIds.Concat(await Resolve(definition.ExcludedNames)).Distinct().ToArray(),
            GuestNames = [], ExcludedNames = []
        };
        if (name != null && await db.CampaignAudiences.AnyAsync(a => a.RestaurantLocationId == locationId && a.Name == name.Trim(), ct))
            throw new ArgumentException("This audience name is already in use.");
        var row = new CampaignAudience { RestaurantLocationId = locationId, Name = name?.Trim(),
            DefinitionJson = JsonSerializer.Serialize(definition) };
        db.Set<CampaignAudience>().Add(row);
        await db.SaveChangesAsync(ct);
        return $"audience:{row.Id:N}";
    }
}
