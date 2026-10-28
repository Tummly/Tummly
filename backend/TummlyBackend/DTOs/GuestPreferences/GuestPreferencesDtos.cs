namespace TummlyBackend.DTOs.GuestPreferences
{
    /// <summary>
    /// Guest preference centre overview. Never carries raw contacts, Master
    /// Guest data, or Location Guest ids.
    /// </summary>
    public sealed record GuestPreferencesOverviewDto(
        string? FirstName,
        IReadOnlyList<GuestPreferenceContactDto> Contacts,
        bool EmailOn,
        bool SmsOn,
        int RestaurantCount
    );

    /// <summary>
    /// A verified contact. <c>Id</c> is the Tummly Guest contact row id.
    /// <c>Masked</c> is the only form of the value that leaves the server.
    /// </summary>
    public sealed record GuestPreferenceContactDto(int Id, string Type, string Masked);

    /// <summary>Only the keys the guest changed are non-null.</summary>
    public sealed record GuestPreferenceChannelsRequest(bool? Email, bool? Sms);

    public sealed record GuestPreferenceRestaurantDto(
        int RestaurantId,
        string Name,
        string? LogoUrl,
        IReadOnlyList<GuestPreferenceLocationDto> Locations
    );

    public sealed record GuestPreferenceLocationDto(int LocationId, string Name, bool Muted);

    public sealed record GuestPreferencesSessionLinkRequest(string? Token);

    public sealed record GuestPreferencesSessionStartRequest(string? Contact);

    public sealed record GuestPreferencesSessionVerifyCodeRequest(string? Contact, string? Code);

    public sealed record GuestPreferencesSessionVerifyEmailRequest(string? V);
}
