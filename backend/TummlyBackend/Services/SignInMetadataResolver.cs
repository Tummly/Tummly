using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class SignInMetadataResolver : ISignInMetadataResolver
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SignInMetadataResolver(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<NewDeviceSignInDetails> ResolveAsync(
            User user,
            SignInContext signInContext,
            CancellationToken cancellationToken = default
        )
        {
            var firstName = ExtractFirstName(user.FullName);
            var signInTime = FormatSignInTime(signInContext.SignedInAtUtc);
            var deviceSummary = UserAgentHelper.Summarize(signInContext.UserAgent);
            var locationSummary = await ResolveApproximateLocationAsync(
                signInContext.IpAddress,
                cancellationToken
            );

            return new NewDeviceSignInDetails
            {
                FirstName = firstName,
                SignInTime = signInTime,
                DeviceSummary = deviceSummary,
                LocationSummary = locationSummary,
            };
        }

        internal static string ExtractFirstName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "there";
            }

            var firstToken = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

            return string.IsNullOrWhiteSpace(firstToken) ? "there" : firstToken;
        }

        internal static string FormatSignInTime(DateTime signedInAtUtc)
        {
            return signedInAtUtc.ToString(
                "d MMMM yyyy 'at' HH:mm 'UTC'",
                CultureInfo.InvariantCulture
            );
        }

        private async Task<string> ResolveApproximateLocationAsync(
            string? ipAddress,
            CancellationToken cancellationToken
        )
        {
            if (!TryParsePublicIp(ipAddress, out var publicIp))
            {
                return "Unknown location";
            }

            try
            {
                var client = _httpClientFactory.CreateClient(
                    SignInMetadataResolverHttpClient.Name
                );

                using var response = await client.GetAsync(
                    $"json/{publicIp}?fields=status,city,regionName,country",
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    return "Unknown location";
                }

                var payload =
                    await response.Content.ReadFromJsonAsync<IpApiResponse>(
                        cancellationToken: cancellationToken
                    );

                if (payload == null || payload.Status != "success")
                {
                    return "Unknown location";
                }

                var parts = new[]
                {
                    payload.City,
                    payload.RegionName,
                    payload.Country,
                }
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return parts.Length == 0
                    ? "Unknown location"
                    : string.Join(", ", parts);
            }
            catch
            {
                return "Unknown location";
            }
        }

        internal static bool TryParsePublicIp(
            string? ipAddress,
            out string publicIp
        )
        {
            publicIp = string.Empty;

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return false;
            }

            if (!IPAddress.TryParse(ipAddress.Trim(), out var parsed))
            {
                return false;
            }

            if (IPAddress.IsLoopback(parsed))
            {
                return false;
            }

            if (parsed.IsIPv4MappedToIPv6)
            {
                parsed = parsed.MapToIPv4();
            }

            var bytes = parsed.GetAddressBytes();

            if (parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                if (bytes[0] == 10)
                {
                    return false;
                }

                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                {
                    return false;
                }

                if (bytes[0] == 192 && bytes[1] == 168)
                {
                    return false;
                }
            }

            publicIp = parsed.ToString();
            return true;
        }

        private sealed class IpApiResponse
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("city")]
            public string? City { get; set; }

            [JsonPropertyName("regionName")]
            public string? RegionName { get; set; }

            [JsonPropertyName("country")]
            public string? Country { get; set; }
        }
    }

    public static class SignInMetadataResolverHttpClient
    {
        public const string Name = "SignInMetadataResolver";

        public static void Configure(HttpClient client)
        {
            client.BaseAddress = new Uri("http://ip-api.com/");
            client.Timeout = TimeSpan.FromSeconds(3);
        }
    }
}
