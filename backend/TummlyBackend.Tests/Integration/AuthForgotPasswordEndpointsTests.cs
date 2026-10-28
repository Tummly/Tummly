using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class AuthForgotPasswordEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string NeutralMessage =
            "If an account exists for this email, we sent a reset link.";

        private readonly IServiceProvider _services;
        private readonly HttpClient _client;

        public AuthForgotPasswordEndpointsTests(TummlyWebApplicationFactory factory)
        {
            // The reset email is stubbed so a known account can be answered without a mail provider.
            var stubbed = factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IEmailService>();
                    services.AddScoped<IEmailService, EmailServiceStubBase>();
                })
            );
            _services = stubbed.Services;
            _client = stubbed.CreateClient();
        }

        [Fact]
        public async Task ForgotPassword_UnknownEmail_ReturnsSameSuccessAsKnownEmail()
        {
            var knownEmail = $"forgot-known-{Guid.NewGuid():N}@example.com";
            await SeedPasswordUserAsync(knownEmail);

            using var known = await PostForgotAsync(knownEmail);
            using var unknown = await PostForgotAsync($"forgot-missing-{Guid.NewGuid():N}@example.com");

            Assert.Equal(HttpStatusCode.OK, known.StatusCode);
            Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
            Assert.Equal(await ReadBodyAsync(known), await ReadBodyAsync(unknown));
        }

        [Fact]
        public async Task ForgotPassword_UnknownEmail_BodyHasNoRawErrorText()
        {
            using var response = await PostForgotAsync($"forgot-none-{Guid.NewGuid():N}@example.com");

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(NeutralMessage, body.GetProperty("message").GetString());
            Assert.DoesNotContain("not found", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        private Task<HttpResponseMessage> PostForgotAsync(string email) =>
            _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        private static async Task<string> ReadBodyAsync(HttpResponseMessage response) =>
            await response.Content.ReadAsStringAsync();

        private async Task SeedPasswordUserAsync(string email)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Users.Add(
                new User
                {
                    Email = email,
                    FullName = "Forgot Owner",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword1!"),
                    Role = "User",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
