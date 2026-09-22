using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ExternalOAuthProviderClient : IExternalOAuthProviderClient
    {
        public const string HttpClientName = nameof(ExternalOAuthProviderClient);

        private const string GoogleAuthorizeUrl =
            "https://accounts.google.com/o/oauth2/v2/auth";
        private const string GoogleTokenUrl = "https://oauth2.googleapis.com/token";
        private const string GoogleUserInfoUrl =
            "https://openidconnect.googleapis.com/userinfo";

        private const string MicrosoftAuthorizeUrl =
            "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        private const string MicrosoftTokenUrl =
            "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        private const string OpenIdScope = "openid email profile";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly ExternalAuthOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalOAuthProviderClient(
            IOptions<ExternalAuthOptions> options,
            IHttpClientFactory httpClientFactory
        )
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
        }

        public string BuildAuthorizationUrl(string provider, string state)
        {
            var (canonical, providerOptions) = ResolveProvider(provider);

            if (string.IsNullOrWhiteSpace(providerOptions.ClientId))
            {
                throw new InvalidOperationException(
                    $"External auth client id is not configured for {canonical}."
                );
            }

            if (string.IsNullOrWhiteSpace(providerOptions.RedirectUri))
            {
                throw new InvalidOperationException(
                    $"External auth redirect URI is not configured for {canonical}."
                );
            }

            var query = new Dictionary<string, string?>
            {
                ["client_id"] = providerOptions.ClientId,
                ["redirect_uri"] = providerOptions.RedirectUri,
                ["response_type"] = "code",
                ["scope"] = OpenIdScope,
                ["state"] = state,
            };

            if (canonical == ExternalAuthProviders.Google)
            {
                query["access_type"] = "online";
                return QueryHelpers.AddQueryString(GoogleAuthorizeUrl, query);
            }

            return QueryHelpers.AddQueryString(MicrosoftAuthorizeUrl, query);
        }

        public async Task<ExternalOAuthProfile> ExchangeCodeAsync(
            string provider,
            string code,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("OAuth authorization code is required.");
            }

            var (canonical, providerOptions) = ResolveProvider(provider);
            EnsureExchangeConfigured(canonical, providerOptions);

            var token = await ExchangeAuthorizationCodeAsync(
                canonical,
                providerOptions,
                code.Trim(),
                cancellationToken
            );

            ExternalOAuthProfile profile =
                canonical == ExternalAuthProviders.Google
                    ? await FetchGoogleProfileAsync(token, cancellationToken)
                    : await FetchMicrosoftProfileAsync(token, cancellationToken);

            profile.Provider = canonical;

            if (!profile.EmailVerified)
            {
                throw new InvalidOperationException("Provider email is not verified.");
            }

            if (string.IsNullOrWhiteSpace(profile.Subject))
            {
                throw new InvalidOperationException("Provider subject is missing.");
            }

            if (string.IsNullOrWhiteSpace(profile.Email))
            {
                throw new InvalidOperationException("Provider email is missing.");
            }

            return profile;
        }

        private (string Canonical, ExternalAuthProviderOptions Options) ResolveProvider(
            string provider
        )
        {
            if (
                string.Equals(
                    provider,
                    ExternalAuthProviders.Google,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return (ExternalAuthProviders.Google, _options.Google);
            }

            if (
                string.Equals(
                    provider,
                    ExternalAuthProviders.Microsoft,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return (ExternalAuthProviders.Microsoft, _options.Microsoft);
            }

            throw new InvalidOperationException($"Unsupported external auth provider '{provider}'.");
        }

        private static void EnsureExchangeConfigured(
            string canonical,
            ExternalAuthProviderOptions providerOptions
        )
        {
            if (
                string.IsNullOrWhiteSpace(providerOptions.ClientId)
                || string.IsNullOrWhiteSpace(providerOptions.ClientSecret)
                || string.IsNullOrWhiteSpace(providerOptions.RedirectUri)
            )
            {
                throw new InvalidOperationException(
                    $"External auth is not fully configured for {canonical}."
                );
            }
        }

        private async Task<TokenResponse> ExchangeAuthorizationCodeAsync(
            string canonical,
            ExternalAuthProviderOptions providerOptions,
            string code,
            CancellationToken cancellationToken
        )
        {
            var tokenUrl =
                canonical == ExternalAuthProviders.Google
                    ? GoogleTokenUrl
                    : MicrosoftTokenUrl;

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = providerOptions.RedirectUri,
                    ["client_id"] = providerOptions.ClientId,
                    ["client_secret"] = providerOptions.ClientSecret,
                }
            );

            using var response = await client.PostAsync(tokenUrl, content, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"OAuth token exchange failed for {canonical}."
                );
            }

            var token = JsonSerializer.Deserialize<TokenResponse>(raw, JsonOptions);
            if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                throw new InvalidOperationException(
                    $"OAuth token response was invalid for {canonical}."
                );
            }

            return token;
        }

        private async Task<ExternalOAuthProfile> FetchGoogleProfileAsync(
            TokenResponse token,
            CancellationToken cancellationToken
        )
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, GoogleUserInfoUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token.AccessToken
            );

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Google userinfo request failed.");
            }

            var payload = await response.Content.ReadFromJsonAsync<GoogleUserInfo>(
                JsonOptions,
                cancellationToken
            );
            if (payload == null)
            {
                throw new InvalidOperationException("Google userinfo response was empty.");
            }

            return new ExternalOAuthProfile
            {
                Subject = payload.Sub ?? string.Empty,
                Email = payload.Email ?? string.Empty,
                EmailVerified = payload.EmailVerified,
                FullName = string.IsNullOrWhiteSpace(payload.Name) ? null : payload.Name.Trim(),
            };
        }

        private async Task<ExternalOAuthProfile> FetchMicrosoftProfileAsync(
            TokenResponse token,
            CancellationToken cancellationToken
        )
        {
            var fromIdToken = TryMapMicrosoftIdToken(token.IdToken);
            if (fromIdToken != null)
            {
                return fromIdToken;
            }

            // Prefer fail-fast: Graph /me has no email_verified and previously
            // always produced EmailVerified=false, which is never useful.
            throw new InvalidOperationException(
                "Microsoft id_token is missing or invalid after token exchange."
            );
        }

        private static ExternalOAuthProfile? TryMapMicrosoftIdToken(string? idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }

            if (!TryParseJwtPayload(idToken, out var payload))
            {
                return null;
            }

            var subject =
                FirstNonEmpty(
                    GetStringClaim(payload, "oid"),
                    GetStringClaim(payload, "sub")
                ) ?? string.Empty;

            var email =
                FirstNonEmpty(
                    GetStringClaim(payload, "email"),
                    GetEmailLikeClaim(payload, "preferred_username")
                ) ?? string.Empty;

            var fullName = GetStringClaim(payload, "name");
            var emailVerifiedClaim = GetNullableBoolClaim(payload, "email_verified");

            // After confidential-client token exchange: email present +
            // email_verified absent ⇒ treat as verified; explicit false still rejects.
            var emailVerified = emailVerifiedClaim switch
            {
                false => false,
                true => true,
                null => !string.IsNullOrWhiteSpace(email),
            };

            return new ExternalOAuthProfile
            {
                Subject = subject,
                Email = email,
                EmailVerified = emailVerified,
                FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
            };
        }

        private static bool TryParseJwtPayload(string jwt, out JsonElement payload)
        {
            payload = default;
            var parts = jwt.Split('.');
            if (parts.Length < 2)
            {
                return false;
            }

            try
            {
                var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                using var doc = JsonDocument.Parse(json);
                payload = doc.RootElement.Clone();
                return true;
            }
            catch
            {
                return false;
            }
        }

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

        private static string? GetStringClaim(JsonElement payload, string name)
        {
            if (
                payload.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String
            )
            {
                return value.GetString();
            }

            return null;
        }

        /// <summary>
        /// preferred_username is often a UPN; only treat it as email when it
        /// contains '@' (basic email shape).
        /// </summary>
        private static string? GetEmailLikeClaim(JsonElement payload, string name)
        {
            var value = GetStringClaim(payload, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Contains('@') ? trimmed : null;
        }

        private static bool? GetNullableBoolClaim(JsonElement payload, string name)
        {
            if (!payload.TryGetProperty(name, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed)
                    ? parsed
                    : null,
                _ => null,
            };
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private sealed class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("id_token")]
            public string? IdToken { get; set; }
        }

        private sealed class GoogleUserInfo
        {
            [JsonPropertyName("sub")]
            public string? Sub { get; set; }

            [JsonPropertyName("email")]
            public string? Email { get; set; }

            [JsonPropertyName("email_verified")]
            public bool EmailVerified { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }
    }
}
