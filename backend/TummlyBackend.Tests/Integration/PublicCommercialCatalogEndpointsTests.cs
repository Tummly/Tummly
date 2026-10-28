using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class PublicCommercialCatalogEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public PublicCommercialCatalogEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!db.CommercialCatalogVersions.Any(v => v.Status == CommercialCatalogStatuses.Published))
            {
                var json = PricebookCatalog.LoadFromDirectory(
                    TestPricebookPaths.PackDirectory()
                );
                var snapshot = json.GetRequired(json.CurrentPricebookId);
                db.CommercialCatalogVersions.Add(
                    CommercialCatalogSeeder.FromSnapshot(snapshot, DateTime.UtcNow)
                );
                db.SaveChanges();
            }
        }

        [Fact]
        public async Task QuoteEndpoint_FractionalLocations_ReturnsInvalidLocationCount()
        {
            using var content = new StringContent(
                "{\"cadence\":\"monthly\",\"locations\":2.5,\"packs\":{}}",
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync(
                "/api/public/commercial-catalog/custom-plan/quote",
                content
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(
                "invalid_location_count",
                body.RootElement.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task CustomUnitRates_PacksCarryServerPrices()
        {
            var response = await _client.GetAsync(
                "/api/public/commercial-catalog/custom-unit-rates"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var packs = body.RootElement.GetProperty("item").GetProperty("packs");
            var teamPack = packs.EnumerateArray().Single(p =>
                p.GetProperty("packKey").GetString() == "team_user"
            );
            Assert.Equal(1, teamPack.GetProperty("packSize").GetInt32());
            Assert.Equal(300, teamPack.GetProperty("monthlyNetPence").GetInt32());
            Assert.Equal(3060, teamPack.GetProperty("annualNetPence").GetInt32());
        }
    }
}
