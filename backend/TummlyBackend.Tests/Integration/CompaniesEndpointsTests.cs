using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Companies;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public sealed class CompaniesSearchWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        public FakeCompaniesHouseSearchService FakeLookup { get; } = new();

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

                var lookupDescriptor = services
                    .SingleOrDefault(service =>
                        service.ServiceType == typeof(ICompaniesHouseSearchService)
                    );

                if (lookupDescriptor is not null)
                {
                    services.Remove(lookupDescriptor);
                }

                services.AddSingleton<ICompaniesHouseSearchService>(FakeLookup);
            });
        }
    }

    public sealed class FakeCompaniesHouseSearchService : ICompaniesHouseSearchService
    {
        public List<CompanySuggestionDto> Suggestions { get; set; } = new();

        public Exception? SuggestException { get; set; }

        public Task<IReadOnlyList<CompanySuggestionDto>> SuggestAsync(
            string query,
            CancellationToken cancellationToken = default
        )
        {
            if (SuggestException is not null)
            {
                throw SuggestException;
            }

            return Task.FromResult<IReadOnlyList<CompanySuggestionDto>>(
                Suggestions
            );
        }
    }

    public class CompaniesEndpointsTests
        : IClassFixture<CompaniesSearchWebApplicationFactory>
    {
        private readonly CompaniesSearchWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CompaniesEndpointsTests(
            CompaniesSearchWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Suggest_returns_401_when_unauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/companies/suggest?q=acme"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Suggest_returns_success_envelope_with_suggestions()
        {
            _factory.FakeLookup.Suggestions =
            [
                new CompanySuggestionDto
                {
                    CompanyNumber = "12345678",
                    Title = "ACME HOSPITALITY LTD",
                    AddressSnippet = "1 High Street, London",
                    CompanyStatus = "active",
                },
            ];

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/companies/suggest?q=acme"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", await CreateJwtAsync());

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(1, body.GetProperty("suggestions").GetArrayLength());
            Assert.Equal(
                "ACME HOSPITALITY LTD",
                body.GetProperty("suggestions")[0].GetProperty("title").GetString()
            );
            Assert.Equal(
                "12345678",
                body.GetProperty("suggestions")[0]
                    .GetProperty("companyNumber")
                    .GetString()
            );
        }

        [Fact]
        public async Task Suggest_rejects_short_queries()
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/companies/suggest?q=acm"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", await CreateJwtAsync());

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Suggest_returns_502_when_upstream_fails()
        {
            _factory.FakeLookup.SuggestException = new HttpRequestException(
                "upstream failed"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/companies/suggest?q=acme"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", await CreateJwtAsync());

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            _factory.FakeLookup.SuggestException = null;
        }

        private async Task<string> CreateJwtAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var email = $"ch-search-{Guid.NewGuid():N}@example.com";
            var user = new User
            {
                FullName = "Company Search Tester",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }
    }
}
