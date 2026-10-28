using Microsoft.Extensions.Configuration;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers;

public class GuestPreferencesSessionTokenTests
{
    private const string Secret = "session-test-secret";

    private static readonly DateTime IssuedAtUtc = new(
        2026,
        10,
        28,
        12,
        0,
        0,
        DateTimeKind.Utc
    );

    [Fact]
    public void RoundTrip_ReturnsIds()
    {
        var token = GuestPreferencesSessionToken.Create(
            tummlyGuestId: 42,
            contactId: 7,
            issuedAtUtc: IssuedAtUtc,
            secret: Secret
        );

        var ok = GuestPreferencesSessionToken.TryVerify(
            token,
            Secret,
            IssuedAtUtc,
            out var tummlyGuestId,
            out var contactId
        );

        Assert.True(ok);
        Assert.Equal(42, tummlyGuestId);
        Assert.Equal(7, contactId);
    }

    [Fact]
    public void Expired_After30Minutes_False()
    {
        var token = GuestPreferencesSessionToken.Create(
            tummlyGuestId: 42,
            contactId: 7,
            issuedAtUtc: IssuedAtUtc,
            secret: Secret
        );

        var ok = GuestPreferencesSessionToken.TryVerify(
            token,
            Secret,
            IssuedAtUtc + GuestPreferencesSessionToken.Lifetime + TimeSpan.FromSeconds(1),
            out var tummlyGuestId,
            out var contactId
        );

        Assert.False(ok);
        Assert.Equal(0, tummlyGuestId);
        Assert.Equal(0, contactId);
    }

    [Fact]
    public void Tampered_False()
    {
        var token = GuestPreferencesSessionToken.Create(
            tummlyGuestId: 42,
            contactId: 7,
            issuedAtUtc: IssuedAtUtc,
            secret: Secret
        );

        // Changing the first base64url character changes the first payload byte,
        // so the HMAC over the payload no longer matches.
        var flipped = (token[0] == 'e' ? 'f' : 'e') + token[1..];

        var ok = GuestPreferencesSessionToken.TryVerify(
            flipped,
            Secret,
            IssuedAtUtc,
            out _,
            out _
        );

        Assert.False(ok);
    }

    [Fact]
    public void UnsubscribeToken_IsRejected()
    {
        var unsubscribeToken = UnsubscribeToken.Create(
            locationGuestId: 42,
            restaurantId: 7,
            issuedAtUtc: IssuedAtUtc,
            secret: Secret
        );

        var ok = GuestPreferencesSessionToken.TryVerify(
            unsubscribeToken,
            Secret,
            IssuedAtUtc,
            out _,
            out _
        );

        Assert.False(ok);
    }

    [Fact]
    public void ResolveSecret_PrefersDedicatedKey()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["GuestPreferences:SessionSigningSecret"] = "session-secret",
                    ["Unsubscribe:SigningSecret"] = "unsubscribe-secret",
                }
            )
            .Build();

        Assert.Equal(
            "session-secret",
            GuestPreferencesSessionToken.ResolveSecret(config)
        );
    }

    [Fact]
    public void ResolveSecret_FallsBackToUnsubscribeSecret()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Unsubscribe:SigningSecret"] = "unsubscribe-secret",
                }
            )
            .Build();

        Assert.Equal(
            "unsubscribe-secret",
            GuestPreferencesSessionToken.ResolveSecret(config)
        );
    }
}
