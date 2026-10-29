using System.Text;
using System.Text.Json.Nodes;
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

        // Keep the payload valid JSON with a changed tg, reuse the original
        // signature, so only the HMAC check can reject it.
        var parts = token.Split('.');
        var json = JsonNode.Parse(Base64UrlDecode(parts[0]))!.AsObject();
        json["tg"] = 99;
        var tamperedPayload = Base64UrlEncode(
            Encoding.UTF8.GetBytes(json.ToJsonString())
        );
        var tampered = tamperedPayload + "." + parts[1];

        var ok = GuestPreferencesSessionToken.TryVerify(
            tampered,
            Secret,
            IssuedAtUtc,
            out var tummlyGuestId,
            out var contactId
        );

        Assert.False(ok);
        Assert.Equal(0, tummlyGuestId);
        Assert.Equal(0, contactId);
    }

    [Fact]
    public void WrongSecret_False()
    {
        var token = GuestPreferencesSessionToken.Create(
            tummlyGuestId: 42,
            contactId: 7,
            issuedAtUtc: IssuedAtUtc,
            secret: Secret
        );

        var ok = GuestPreferencesSessionToken.TryVerify(
            token,
            "a-different-secret",
            IssuedAtUtc,
            out _,
            out _
        );

        Assert.False(ok);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
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

    [Fact]
    public void FallsBackToSharedSecret_ProductionWithoutDedicatedKey_True()
    {
        var config = ConfigWith(new Dictionary<string, string?>
        {
            ["Unsubscribe:SigningSecret"] = "unsubscribe-secret",
        });

        Assert.True(GuestPreferencesSessionToken.FallsBackToSharedSecret(isProduction: true, config));
    }

    [Fact]
    public void FallsBackToSharedSecret_ProductionWithBlankDedicatedKey_True()
    {
        var config = ConfigWith(new Dictionary<string, string?>
        {
            ["GuestPreferences:SessionSigningSecret"] = "   ",
        });

        Assert.True(GuestPreferencesSessionToken.FallsBackToSharedSecret(isProduction: true, config));
    }

    [Fact]
    public void FallsBackToSharedSecret_ProductionWithDedicatedKey_False()
    {
        var config = ConfigWith(new Dictionary<string, string?>
        {
            ["GuestPreferences:SessionSigningSecret"] = "session-secret",
        });

        Assert.False(GuestPreferencesSessionToken.FallsBackToSharedSecret(isProduction: true, config));
    }

    [Fact]
    public void FallsBackToSharedSecret_NonProductionWithoutDedicatedKey_False()
    {
        var config = ConfigWith(new Dictionary<string, string?>());

        Assert.False(GuestPreferencesSessionToken.FallsBackToSharedSecret(isProduction: false, config));
    }

    private static IConfiguration ConfigWith(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
