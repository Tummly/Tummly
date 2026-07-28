using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public sealed class GuestSttWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        public FakeSpeechToTextProvider FakeStt { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var dbDescriptors = services
                    .Where(service =>
                        service.ServiceType ==
                            typeof(DbContextOptions<ApplicationDbContext>)
                        || service.ServiceType == typeof(ApplicationDbContext)
                    )
                    .ToList();

                foreach (var descriptor in dbDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(warning =>
                        warning.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                    );
                });

                foreach (var descriptor in services
                    .Where(service =>
                        service.ServiceType == typeof(ISpeechToTextProvider)
                        || service.ServiceType ==
                            typeof(FakeSpeechToTextProvider)
                    )
                    .ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(FakeStt);
                services.AddSingleton<ISpeechToTextProvider>(FakeStt);
            });
        }
    }

    public class GuestSttEndpointsTests
        : IClassFixture<GuestSttWebApplicationFactory>
    {
        private readonly GuestSttWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestSttEndpointsTests(GuestSttWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Transcribe_ReturnsText_WithoutPersistingFeedbackOrAudio()
        {
            _factory.FakeStt.Reset();
            _factory.FakeStt.SucceedWith(
                "The soup was cold and service was slow."
            );

            var linkToken = "stt-succeed-token-1234567";
            await SeedLocationAsync(linkToken);

            var audioBytes = Encoding.UTF8.GetBytes("fake-webm-audio-bytes");
            var response = await PostAudioAsync(linkToken, audioBytes);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "The soup was cold and service was slow.",
                body.GetProperty("text").GetString()
            );

            Assert.Equal(1, _factory.FakeStt.TranscribeCallCount);
            Assert.Equal(audioBytes, _factory.FakeStt.LastAudioBytes);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Empty(context.Feedbacks);
        }

        [Fact]
        public async Task Transcribe_EmptySpeech_ReturnsDistinctError()
        {
            _factory.FakeStt.Reset();
            _factory.FakeStt.EmptySpeech();

            var linkToken = "stt-empty-token-1234567";
            await SeedLocationAsync(linkToken);

            var response = await PostAudioAsync(
                linkToken,
                Encoding.UTF8.GetBytes("near-silence")
            );

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("empty_speech", body.GetProperty("code").GetString());
            Assert.Equal(
                "We didn't catch any speech. Try again or type your feedback.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task Transcribe_RateLimit_Returns429AfterTenAttempts()
        {
            _factory.FakeStt.Reset();
            _factory.FakeStt.SucceedWith("ok");

            var linkToken = "stt-rate-token-1234567";
            await SeedLocationAsync(linkToken);

            for (var i = 0; i < 10; i++)
            {
                var ok = await PostAudioAsync(
                    linkToken,
                    Encoding.UTF8.GetBytes($"clip-{i}")
                );
                Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            }

            var limited = await PostAudioAsync(
                linkToken,
                Encoding.UTF8.GetBytes("clip-over-limit")
            );

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);

            var body = await ReadJsonAsync(limited);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Too many voice attempts from this link. Try typing instead.",
                body.GetProperty("message").GetString()
            );
            Assert.Equal(10, _factory.FakeStt.TranscribeCallCount);
        }

        [Fact]
        public async Task Transcribe_UnknownToken_Returns404()
        {
            _factory.FakeStt.Reset();
            var response = await PostAudioAsync(
                "missing-stt-token-1234567",
                Encoding.UTF8.GetBytes("audio")
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, _factory.FakeStt.TranscribeCallCount);
        }

        [Fact]
        public async Task Transcribe_MissingAudio_Returns400()
        {
            _factory.FakeStt.Reset();
            var linkToken = "stt-missing-audio-token-1234567";
            await SeedLocationAsync(linkToken);

            using var content = new MultipartFormDataContent();
            var response = await _client.PostAsync(
                $"/api/scan/{linkToken}/stt",
                content
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, _factory.FakeStt.TranscribeCallCount);
        }

        private async Task SeedLocationAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var user = new User
            {
                FullName = "STT Owner",
                Email = $"{linkToken}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "STT Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.Add(new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = linkToken,
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        private async Task<HttpResponseMessage> PostAudioAsync(
            string token,
            byte[] audioBytes
        )
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(audioBytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/webm");
            content.Add(fileContent, "audio", "clip.webm");

            return await _client.PostAsync(
                $"/api/scan/{token}/stt",
                content
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
