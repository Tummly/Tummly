using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Address;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Integration
{
    public sealed class AddressLookupWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        public FakeAddressLookupService FakeLookup { get; } = new();

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
                        service.ServiceType == typeof(IAddressLookupService)
                    );

                if (lookupDescriptor is not null)
                {
                    services.Remove(lookupDescriptor);
                }

                services.AddSingleton<IAddressLookupService>(FakeLookup);
            });
        }
    }

    public sealed class FakeAddressLookupService : IAddressLookupService
    {
        public List<AddressSuggestionDto> Suggestions { get; set; } = new();

        public AddressResolveResultDto? ResolveResult { get; set; }

        public Task<IReadOnlyList<AddressSuggestionDto>> SuggestAsync(
            string query,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<IReadOnlyList<AddressSuggestionDto>>(
                Suggestions
            );
        }

        public Task<AddressResolveResultDto?> ResolvePostcodeAsync(
            string postcode,
            string? addressHint = null,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(ResolveResult);
        }

        public AddressPremiseDto? ResolveSuggestionResult { get; set; }

        public Task<AddressPremiseDto?> ResolveSuggestionAsync(
            string suggestionId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(ResolveSuggestionResult);
        }
    }

    public class AddressEndpointsTests
        : IClassFixture<AddressLookupWebApplicationFactory>
    {
        private readonly AddressLookupWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AddressEndpointsTests(
            AddressLookupWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Suggest_returns_success_envelope_with_suggestions()
        {
            _factory.FakeLookup.Suggestions =
            [
                new AddressSuggestionDto
                {
                    Id = "paf_1",
                    Label = "125 High Street, Manchester, M1 4AB",
                    Address = "125 High Street, Manchester",
                    Postcode = "M1 4AB",
                },
            ];

            var response = await _client.GetAsync(
                "/api/address/suggest?q=125%20High"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(1, body.GetProperty("suggestions").GetArrayLength());
            Assert.Equal(
                "125 High Street, Manchester, M1 4AB",
                body.GetProperty("suggestions")[0].GetProperty("label").GetString()
            );
        }

        [Fact]
        public async Task ResolveSuggestion_returns_success_envelope_with_address()
        {
            _factory.FakeLookup.ResolveSuggestionResult = new AddressPremiseDto
            {
                Address = "125 High Street, Manchester",
                PostTown = "Manchester",
                Postcode = "M1 4AB",
            };

            var response = await _client.GetAsync(
                "/api/address/resolve-suggestion?id=paf_1"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "125 High Street, Manchester",
                body.GetProperty("address").GetString()
            );
            Assert.Equal("M1 4AB", body.GetProperty("postcode").GetString());
            Assert.Equal("Manchester", body.GetProperty("postTown").GetString());
        }

        [Fact]
        public async Task ResolveSuggestion_rejects_missing_id()
        {
            var response = await _client.GetAsync("/api/address/resolve-suggestion");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Suggest_rejects_short_queries()
        {
            var response = await _client.GetAsync("/api/address/suggest?q=12");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Resolve_returns_success_envelope_with_premises()
        {
            _factory.FakeLookup.ResolveResult = new AddressResolveResultDto
            {
                Postcode = "M1 4AB",
                Address = "125 High Street, Manchester",
                Premises =
                [
                    new AddressPremiseDto
                    {
                        Address = "125 High Street, Manchester",
                        Postcode = "M1 4AB",
                    },
                ],
            };

            var response = await _client.GetAsync(
                "/api/address/resolve?postcode=M1%204AB"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "125 High Street, Manchester",
                body.GetProperty("address").GetString()
            );
        }

        [Fact]
        public async Task Resolve_rejects_invalid_postcode_format()
        {
            var response = await _client.GetAsync(
                "/api/address/resolve?postcode=invalid"
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
