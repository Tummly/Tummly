using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class OperatorSttEndpointsTests : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OperatorSttEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private FakeSpeechToTextProvider FakeStt =>
            _factory.Services.GetRequiredService<FakeSpeechToTextProvider>();

        [Fact]
        public async Task Transcribe_Returns401_WhenUnauthenticated()
        {
            FakeStt.Reset();
            var response = await PostAudioAsync(
                jwt: null,
                Encoding.UTF8.GetBytes("fake-webm-audio-bytes")
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(0, FakeStt.TranscribeCallCount);
        }

        [Fact]
        public async Task Transcribe_Returns403_WhenPendingActivation()
        {
            FakeStt.Reset();
            var jwt = await SeedPendingOperatorAsync();

            var response = await PostAudioAsync(
                jwt,
                Encoding.UTF8.GetBytes("fake-webm-audio-bytes")
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("activationRequired").GetBoolean());
            Assert.Equal(0, FakeStt.TranscribeCallCount);
        }

        [Fact]
        public async Task Transcribe_ReturnsText_WithFakeProvider()
        {
            FakeStt.Reset();
            FakeStt.SucceedWith("Summarise Camden this week");
            var owner = await SeedOwnerAsync("operator-stt-ok@example.com");

            var audioBytes = Encoding.UTF8.GetBytes("fake-webm-audio-bytes");
            var response = await PostAudioAsync(owner.Jwt, audioBytes);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Summarise Camden this week",
                body.GetProperty("text").GetString()
            );
            Assert.Equal(1, FakeStt.TranscribeCallCount);
            Assert.Equal(audioBytes, FakeStt.LastAudioBytes);
        }

        [Fact]
        public async Task Transcribe_EmptySpeech_ReturnsDistinctError()
        {
            FakeStt.Reset();
            FakeStt.EmptySpeech();
            var owner = await SeedOwnerAsync("operator-stt-empty@example.com");

            var response = await PostAudioAsync(
                owner.Jwt,
                Encoding.UTF8.GetBytes("near-silence")
            );

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("empty_speech", body.GetProperty("code").GetString());
            Assert.Equal(
                "We didn't catch any speech. Try again or type your question.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task Transcribe_Failure_ReturnsDistinctError()
        {
            FakeStt.Reset();
            FakeStt.Fail();
            var owner = await SeedOwnerAsync("operator-stt-fail@example.com");

            var response = await PostAudioAsync(
                owner.Jwt,
                Encoding.UTF8.GetBytes("unreadable-audio")
            );

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("stt_failure", body.GetProperty("code").GetString());
            Assert.Equal(
                "We couldn't transcribe that recording. Try again or type your question.",
                body.GetProperty("message").GetString()
            );
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Assistant Owner",
                Email = email,
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
                Name = "Assistant Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }

        private async Task<string> SeedPendingOperatorAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                Email = "pending-stt@example.com",
                FullName = "Pending Operator",
                PhoneNumber = "5551234568",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                ActivationCodeHash = ActivationCodeHelper.HashCode("ABCD2345"),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
            return jwt;
        }

        private async Task<HttpResponseMessage> PostAudioAsync(
            string? jwt,
            byte[] audioBytes
        )
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(audioBytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/webm");
            content.Add(fileContent, "audio", "clip.webm");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/stt"
            )
            {
                Content = content,
            };
            if (jwt != null)
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
            }

            return await _client.SendAsync(request);
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
