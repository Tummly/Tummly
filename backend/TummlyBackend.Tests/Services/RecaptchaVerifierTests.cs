using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class RecaptchaVerifierTests
    {
        [Fact]
        public async Task Verify_WhenSecretUnset_Skips()
        {
            var verifier = CreateVerifier(
                secret: "",
                handler: StubHandler.Sync(_ =>
                    throw new InvalidOperationException("should not call Google")
                )
            );

            var result = await verifier.VerifyGuestFeedbackAsync(
                "any-token",
                "203.0.113.1"
            );

            Assert.Equal(RecaptchaVerifyStatus.Skipped, result.Status);
        }

        [Fact]
        public async Task Verify_WhenSecretSetAndTokenMissing_Fails()
        {
            var verifier = CreateVerifier(
                secret: "test-secret",
                handler: StubHandler.Sync(_ =>
                    throw new InvalidOperationException("should not call Google")
                )
            );

            var result = await verifier.VerifyGuestFeedbackAsync(
                "   ",
                null
            );

            Assert.Equal(RecaptchaVerifyStatus.MissingToken, result.Status);
            Assert.Contains("security check", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Verify_WhenGoogleSaysSuccess_Passes()
        {
            var verifier = CreateVerifier(
                secret: "test-secret",
                handler: StubHandler.Sync(_ =>
                    JsonResponse(
                        """{"success":true,"score":0.9,"action":"guest_feedback"}"""
                    )
                )
            );

            var result = await verifier.VerifyGuestFeedbackAsync(
                "token-abc",
                "203.0.113.1"
            );

            Assert.Equal(RecaptchaVerifyStatus.Passed, result.Status);
        }

        [Fact]
        public async Task Verify_WhenScoreTooLow_Fails()
        {
            var verifier = CreateVerifier(
                secret: "test-secret",
                minScore: 0.5,
                handler: StubHandler.Sync(_ =>
                    JsonResponse(
                        """{"success":true,"score":0.1,"action":"guest_feedback"}"""
                    )
                )
            );

            var result = await verifier.VerifyGuestFeedbackAsync(
                "token-abc",
                null
            );

            Assert.Equal(RecaptchaVerifyStatus.Failed, result.Status);
        }

        [Fact]
        public async Task Verify_WhenActionMismatch_Fails()
        {
            var verifier = CreateVerifier(
                secret: "test-secret",
                handler: StubHandler.Sync(_ =>
                    JsonResponse(
                        """{"success":true,"score":0.9,"action":"other"}"""
                    )
                )
            );

            var result = await verifier.VerifyGuestFeedbackAsync(
                "token-abc",
                null
            );

            Assert.Equal(RecaptchaVerifyStatus.Failed, result.Status);
        }

        [Fact]
        public async Task Verify_PostsSecretResponseAndRemoteIp()
        {
            string? body = null;
            var verifier = CreateVerifier(
                secret: "test-secret",
                handler: StubHandler.Async(async request =>
                {
                    body = await request.Content!.ReadAsStringAsync();
                    return JsonResponse(
                        """{"success":true,"score":0.9,"action":"guest_feedback"}"""
                    );
                })
            );

            await verifier.VerifyGuestFeedbackAsync(
                "token-abc",
                "198.51.100.2"
            );

            Assert.NotNull(body);
            Assert.Contains("secret=test-secret", body, StringComparison.Ordinal);
            Assert.Contains("response=token-abc", body, StringComparison.Ordinal);
            Assert.Contains(
                "remoteip=198.51.100.2",
                body,
                StringComparison.Ordinal
            );
        }

        private static RecaptchaVerifier CreateVerifier(
            string secret,
            HttpMessageHandler handler,
            double minScore = 0.5
        )
        {
            var factory = new StubHttpClientFactory(handler);
            var options = Options.Create(
                new RecaptchaSettings
                {
                    SecretKey = secret,
                    MinScore = minScore,
                    ExpectedAction = "guest_feedback",
                    VerifyUrl =
                        "https://www.google.com/recaptcha/api/siteverify",
                }
            );
            return new RecaptchaVerifier(factory, options);
        }

        private static HttpResponseMessage JsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                ),
            };
        }

        private sealed class StubHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpMessageHandler _handler;

            public StubHttpClientFactory(HttpMessageHandler handler)
            {
                _handler = handler;
            }

            public HttpClient CreateClient(string name)
            {
                return new HttpClient(_handler, disposeHandler: false)
                {
                    BaseAddress = null,
                };
            }
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _respond;

            public static StubHandler Sync(
                Func<HttpRequestMessage, HttpResponseMessage> respond
            ) => new(request => Task.FromResult(respond(request)));

            public static StubHandler Async(
                Func<HttpRequestMessage, Task<HttpResponseMessage>> respond
            ) => new(respond);

            private StubHandler(
                Func<HttpRequestMessage, Task<HttpResponseMessage>> respond
            )
            {
                _respond = respond;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            ) => _respond(request);
        }
    }
}
