using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class RecaptchaVerifier : IRecaptchaVerifier
    {
        public const string HttpClientName = "Recaptcha";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RecaptchaSettings _settings;

        public RecaptchaVerifier(
            IHttpClientFactory httpClientFactory,
            IOptions<RecaptchaSettings> options
        )
        {
            _httpClientFactory = httpClientFactory;
            _settings = options.Value;
        }

        public async Task<RecaptchaVerifyResult> VerifyGuestFeedbackAsync(
            string? token,
            string? remoteIp,
            CancellationToken cancellationToken = default
        )
        {
            var secret = _settings.SecretKey?.Trim();
            if (string.IsNullOrEmpty(secret))
            {
                return new RecaptchaVerifyResult(
                    RecaptchaVerifyStatus.Skipped,
                    null
                );
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return new RecaptchaVerifyResult(
                    RecaptchaVerifyStatus.MissingToken,
                    "Complete the security check and try again."
                );
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var content = new FormUrlEncodedContent(
                BuildForm(secret, token.Trim(), remoteIp)
            );

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(
                    _settings.VerifyUrl,
                    content,
                    cancellationToken
                );
            }
            catch (Exception)
            {
                return new RecaptchaVerifyResult(
                    RecaptchaVerifyStatus.Failed,
                    "Security check failed. Please try again."
                );
            }

            if (!response.IsSuccessStatusCode)
            {
                return new RecaptchaVerifyResult(
                    RecaptchaVerifyStatus.Failed,
                    "Security check failed. Please try again."
                );
            }

            var body = await response.Content
                .ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken);

            if (body == null || !body.Success)
            {
                return new RecaptchaVerifyResult(
                    RecaptchaVerifyStatus.Failed,
                    "Security check failed. Please try again."
                );
            }

            var expectedAction = (_settings.ExpectedAction ?? string.Empty)
                .Trim();
            if (
                expectedAction.Length > 0
                && !string.Equals(
                    body.Action?.Trim(),
                    expectedAction,
                    StringComparison.Ordinal
                )
            )
            {
                return new RecaptchaVerifyResult(
                    RecaptchaVerifyStatus.Failed,
                    "Security check failed. Please try again."
                );
            }

            var minScore = _settings.MinScore;
            if (minScore < 0)
            {
                minScore = 0;
            }
            else if (minScore > 1)
            {
                minScore = 1;
            }

            if (body.Score < minScore)
            {
                return new RecaptchaVerifyResult(
                    RecaptchaVerifyStatus.Failed,
                    "Security check failed. Please try again."
                );
            }

            return new RecaptchaVerifyResult(
                RecaptchaVerifyStatus.Passed,
                null
            );
        }

        private static List<KeyValuePair<string, string>> BuildForm(
            string secret,
            string token,
            string? remoteIp
        )
        {
            var fields = new List<KeyValuePair<string, string>>
            {
                new("secret", secret),
                new("response", token),
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                fields.Add(new("remoteip", remoteIp.Trim()));
            }

            return fields;
        }

        private sealed class SiteVerifyResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("score")]
            public double Score { get; set; }

            [JsonPropertyName("action")]
            public string? Action { get; set; }
        }
    }
}
