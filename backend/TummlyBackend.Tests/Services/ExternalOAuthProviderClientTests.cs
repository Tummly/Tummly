using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class ExternalOAuthProviderClientTests
    {
        [Fact]
        public void BuildAuthorizationUrl_Google_IncludesClientIdAndState()
        {
            var options = Options.Create(new ExternalAuthOptions
            {
                Google = new ExternalAuthProviderOptions
                {
                    ClientId = "g-client",
                    ClientSecret = "g-secret",
                    RedirectUri = "https://api.example/api/auth/external/google/callback",
                },
            });
            var client = new ExternalOAuthProviderClient(
                options,
                new UnusedHttpClientFactory()
            );
            var url = client.BuildAuthorizationUrl(
                ExternalAuthProviders.Google,
                "state-abc"
            );
            Assert.Contains("client_id=g-client", url);
            Assert.Contains("state=state-abc", url);
            Assert.Contains("accounts.google.com", url);
        }

        [Fact]
        public async Task ExchangeCode_Microsoft_IdTokenWithoutEmailVerified_TreatsEmailVerified()
        {
            var idToken = BuildUnsignedJwt(
                new Dictionary<string, object>
                {
                    ["oid"] = "ms-oid-1",
                    ["email"] = "user@contoso.com",
                    ["name"] = "Contoso User",
                }
            );

            var handler = new ScriptedHandler(req =>
            {
                Assert.Contains("oauth2/v2.0/token", req.RequestUri!.AbsoluteUri);
                return JsonResponse(
                    new { access_token = "access-1", id_token = idToken }
                );
            });

            var client = CreateMicrosoftClient(handler);
            var profile = await client.ExchangeCodeAsync(
                ExternalAuthProviders.Microsoft,
                "auth-code"
            );

            Assert.Equal("ms-oid-1", profile.Subject);
            Assert.Equal("user@contoso.com", profile.Email);
            Assert.True(profile.EmailVerified);
            Assert.Equal("Contoso User", profile.FullName);
        }

        [Fact]
        public async Task ExchangeCode_Microsoft_EmailVerifiedFalse_Throws()
        {
            var idToken = BuildUnsignedJwt(
                new Dictionary<string, object>
                {
                    ["oid"] = "ms-oid-2",
                    ["email"] = "user@contoso.com",
                    ["email_verified"] = false,
                }
            );

            var handler = new ScriptedHandler(_ =>
                JsonResponse(new { access_token = "access-1", id_token = idToken })
            );

            var client = CreateMicrosoftClient(handler);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.ExchangeCodeAsync(ExternalAuthProviders.Microsoft, "auth-code")
            );
        }

        [Fact]
        public async Task ExchangeCode_Microsoft_PreferredUsernameWithAt_UsedAsEmail()
        {
            var idToken = BuildUnsignedJwt(
                new Dictionary<string, object>
                {
                    ["oid"] = "ms-oid-3",
                    ["preferred_username"] = "user@contoso.com",
                    ["name"] = "Contoso User",
                }
            );

            var handler = new ScriptedHandler(_ =>
                JsonResponse(new { access_token = "access-1", id_token = idToken })
            );

            var client = CreateMicrosoftClient(handler);
            var profile = await client.ExchangeCodeAsync(
                ExternalAuthProviders.Microsoft,
                "auth-code"
            );

            Assert.Equal("user@contoso.com", profile.Email);
            Assert.True(profile.EmailVerified);
        }

        [Fact]
        public async Task ExchangeCode_Microsoft_PreferredUsernameWithoutAt_ThrowsMissingEmail()
        {
            var idToken = BuildUnsignedJwt(
                new Dictionary<string, object>
                {
                    ["oid"] = "ms-oid-4",
                    ["preferred_username"] = "DOMAIN\\user",
                    ["name"] = "Contoso User",
                }
            );

            var handler = new ScriptedHandler(_ =>
                JsonResponse(new { access_token = "access-1", id_token = idToken })
            );

            var client = CreateMicrosoftClient(handler);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.ExchangeCodeAsync(ExternalAuthProviders.Microsoft, "auth-code")
            );

            Assert.Equal("Provider email is not verified.", ex.Message);
        }

        private static ExternalOAuthProviderClient CreateMicrosoftClient(
            HttpMessageHandler handler
        )
        {
            var options = Options.Create(
                new ExternalAuthOptions
                {
                    Microsoft = new ExternalAuthProviderOptions
                    {
                        ClientId = "ms-client",
                        ClientSecret = "ms-secret",
                        RedirectUri =
                            "https://api.example/api/auth/external/microsoft/callback",
                    },
                }
            );

            return new ExternalOAuthProviderClient(
                options,
                new FixedHttpClientFactory(handler)
            );
        }

        private static string BuildUnsignedJwt(Dictionary<string, object> claims)
        {
            static string B64Url(byte[] bytes) =>
                Convert
                    .ToBase64String(bytes)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');

            var header = B64Url(
                Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}""")
            );
            var payload = B64Url(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(claims))
            );
            return $"{header}.{payload}.sig";
        }

        private static HttpResponseMessage JsonResponse(object body) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            };

        private sealed class UnusedHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) =>
                throw new NotImplementedException();
        }

        private sealed class FixedHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpMessageHandler _handler;

            public FixedHttpClientFactory(HttpMessageHandler handler) =>
                _handler = handler;

            public HttpClient CreateClient(string name) => new(_handler, false);
        }

        private sealed class ScriptedHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public ScriptedHandler(
                Func<HttpRequestMessage, HttpResponseMessage> responder
            ) => _responder = responder;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            ) => Task.FromResult(_responder(request));
        }
    }
}
