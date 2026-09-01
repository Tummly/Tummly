using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class SetupAccountCityEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SetupAccountCityEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SetupAccount_PersistsActiveLocationWithCity()
        {
            await SeedInviteAsync(
                email: "setup-city-ok@example.com",
                token: "setup-city-ok-token",
                businessName: "City Ok Cafe"
            );

            var response = await _client.PostAsJsonAsync(
                "/api/auth/setup-account",
                new
                {
                    token = "setup-city-ok-token",
                    password = "Password1!",
                    confirmPassword = "Password1!",
                    fullName = "City Owner",
                    groupName = "City Ok Cafe",
                    businessCategory = "takeaway",
                    primaryPhone = "07911123456",
                    locations = new[]
                    {
                        new
                        {
                            locationName = "Main",
                            address = "1 High Street",
                            city = "Leeds",
                            postcode = "LS1 1AA",
                        },
                    },
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .SingleAsync(row =>
                    row.Restaurant!.Name == "City Ok Cafe"
                );

            Assert.Equal("Leeds", location.City);
            Assert.Equal("LS1 1AA", location.Postcode);
            Assert.Equal(
                LocationLifecycleStatus.Active,
                location.LifecycleStatus
            );
        }

        [Fact]
        public async Task SetupAccount_RejectsMissingCity()
        {
            await SeedInviteAsync(
                email: "setup-city-missing@example.com",
                token: "setup-city-missing-token",
                businessName: "City Missing Cafe"
            );

            var response = await _client.PostAsJsonAsync(
                "/api/auth/setup-account",
                new
                {
                    token = "setup-city-missing-token",
                    password = "Password1!",
                    confirmPassword = "Password1!",
                    fullName = "City Owner",
                    groupName = "City Missing Cafe",
                    businessCategory = "takeaway",
                    primaryPhone = "07911123456",
                    locations = new[]
                    {
                        new
                        {
                            locationName = "Main",
                            address = "1 High Street",
                            postcode = "LS1 1AA",
                        },
                    },
                }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.Restaurants.AnyAsync(row =>
                    row.Name == "City Missing Cafe"
                )
            );
        }

        private async Task SeedInviteAsync(
            string email,
            string token,
            string businessName
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            context.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = businessName,
                    BusinessCategory = "takeaway",
                    Locations = "1",
                    FullName = "City Owner",
                    Email = email,
                    Mobile = "07911123456",
                    MainLocation = "1 High Street",
                    TownCity = "Leeds",
                    Postcode = "LS1 1AA",
                    Role = "Owner",
                    Goal = "Grow",
                    TermsAccepted = true,
                    IsEmailVerified = true,
                    IsApproved = true,
                    Status = TrialRequestStatus.Approved,
                    ApprovalToken = token,
                    InviteExpiresAt = DateTime.UtcNow.AddDays(7),
                    IsAccountCreated = false,
                    AccountType = "Single",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
