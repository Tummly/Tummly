using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class BillingCreditsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public BillingCreditsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Returns401_WithoutJwt()
        {
            var response = await _client.GetAsync("/api/billing-credits");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Get_Returns403_ForStaff()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.StaffJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Get_ReturnsPilotSnapshot_ForAdminView()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("actorCanManage").GetBoolean());
            Assert.False(
                body.GetProperty("actorCanPersistBillingContacts").GetBoolean()
            );
            Assert.Equal("Admin", body.GetProperty("actorPermissionRole").GetString());

            var plan = body.GetProperty("planSubscription");
            Assert.Equal("Pilot", plan.GetProperty("subscriptionPlan").GetString());
            Assert.Equal("Pilot", plan.GetProperty("billingStatus").GetString());
            Assert.Equal(500, plan.GetProperty("emailCreditsRemaining").GetInt32());
            Assert.Equal(20, plan.GetProperty("smsCreditsRemaining").GetInt32());
            Assert.Equal(20, plan.GetProperty("aiCreditsRemaining").GetInt32());
            Assert.True(plan.GetProperty("isPilot").GetBoolean());

            var contacts = body.GetProperty("billingContacts");
            Assert.True(contacts.GetProperty("lowCreditAlerts").GetProperty("owner").GetBoolean());
            Assert.False(contacts.GetProperty("lowCreditAlerts").GetProperty("admin").GetBoolean());
            Assert.True(
                contacts.GetProperty("lowCreditAlerts").GetProperty("billingContact").GetBoolean()
            );
        }

        [Fact]
        public async Task PutBillingContacts_Returns403_ForView()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedPutBillingContacts(
                seeded.AdminJwt,
                new
                {
                    billingContactUserId = seeded.OwnerUserId,
                    billingEmail = "billing@example.com",
                    lowCreditAlerts = new
                    {
                        owner = true,
                        admin = false,
                        billingContact = true,
                    },
                    paymentFailureAlerts = new
                    {
                        owner = true,
                        billingContact = true,
                    },
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PutBillingContacts_Returns403_ForBillingAdmin()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedPutBillingContacts(
                seeded.BillingAdminJwt,
                new
                {
                    billingContactUserId = seeded.OwnerUserId,
                    billingEmail = "billing@example.com",
                    lowCreditAlerts = new
                    {
                        owner = true,
                        admin = false,
                        billingContact = true,
                    },
                    paymentFailureAlerts = new
                    {
                        owner = true,
                        billingContact = true,
                    },
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PutBillingContacts_OwnerWrite_UpdatesNominationMailboxAndTicks()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedPutBillingContacts(
                seeded.OwnerJwt,
                new
                {
                    billingContactUserId = seeded.AdminUserId,
                    billingEmail = "invoices@example.com",
                    lowCreditAlerts = new
                    {
                        owner = false,
                        admin = true,
                        billingContact = false,
                    },
                    paymentFailureAlerts = new
                    {
                        owner = false,
                        billingContact = true,
                    },
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var contacts = body.GetProperty("billingContacts");
            Assert.Equal(
                seeded.AdminUserId,
                contacts.GetProperty("billingContactUserId").GetInt32()
            );
            Assert.Equal(
                "invoices@example.com",
                contacts.GetProperty("billingEmail").GetString()
            );
            Assert.False(contacts.GetProperty("lowCreditAlerts").GetProperty("owner").GetBoolean());
            Assert.True(contacts.GetProperty("lowCreditAlerts").GetProperty("admin").GetBoolean());
            Assert.False(
                contacts.GetProperty("lowCreditAlerts").GetProperty("billingContact").GetBoolean()
            );
            Assert.False(
                contacts.GetProperty("paymentFailureAlerts").GetProperty("owner").GetBoolean()
            );
            Assert.True(
                contacts.GetProperty("paymentFailureAlerts").GetProperty("billingContact").GetBoolean()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = await context.Restaurants
                .SingleAsync(r => r.Id == seeded.RestaurantId);
            Assert.Equal(seeded.AdminUserId, restaurant.BillingContactUserId);

            var billingAccount = await context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal("invoices@example.com", billingAccount.BillingEmail);
            Assert.False(billingAccount.LowCreditAlertOwner);
            Assert.True(billingAccount.LowCreditAlertAdmin);
            Assert.False(billingAccount.LowCreditAlertBillingContact);
            Assert.False(billingAccount.PaymentFailureAlertOwner);
            Assert.True(billingAccount.PaymentFailureAlertBillingContact);
        }

        [Fact]
        public async Task PutKeyContacts_DoesNotClearBillingMailboxOrTicks()
        {
            var seeded = await SeedWorkspaceAsync();

            using (var billingRequest = AuthorizedPutBillingContacts(
                seeded.OwnerJwt,
                new
                {
                    billingContactUserId = seeded.OwnerUserId,
                    billingEmail = "mailbox@example.com",
                    lowCreditAlerts = new
                    {
                        owner = true,
                        admin = true,
                        billingContact = false,
                    },
                    paymentFailureAlerts = new
                    {
                        owner = false,
                        billingContact = false,
                    },
                }
            ))
            {
                var billingResponse = await _client.SendAsync(billingRequest);
                Assert.Equal(HttpStatusCode.OK, billingResponse.StatusCode);
            }

            using var keyContactsRequest = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/key-contacts"
            );
            keyContactsRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            keyContactsRequest.Content = JsonContent.Create(new
            {
                billingContactUserId = seeded.AdminUserId,
                privacyContactUserId = seeded.OwnerUserId,
                supportContactUserId = seeded.OwnerUserId,
            });

            var keyContactsResponse = await _client.SendAsync(keyContactsRequest);
            Assert.Equal(HttpStatusCode.OK, keyContactsResponse.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = await context.Restaurants
                .SingleAsync(r => r.Id == seeded.RestaurantId);
            Assert.Equal(seeded.AdminUserId, restaurant.BillingContactUserId);

            var billingAccount = await context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal("mailbox@example.com", billingAccount.BillingEmail);
            Assert.True(billingAccount.LowCreditAlertOwner);
            Assert.True(billingAccount.LowCreditAlertAdmin);
            Assert.False(billingAccount.LowCreditAlertBillingContact);
            Assert.False(billingAccount.PaymentFailureAlertOwner);
            Assert.False(billingAccount.PaymentFailureAlertBillingContact);
        }

        private async Task<Seeded> SeedWorkspaceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = AddUser(context, "Owner TwentyOne", "Owner");
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Billing Venue 21",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(restaurant.Id)
            );

            owner.SelectedRestaurantId = restaurant.Id;

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            AddMembership(
                context,
                owner.Id,
                restaurant.Id,
                PermissionRoles.Owner,
                LocationScopeKind.AllLocations,
                "[]"
            );

            var admin = AddUser(context, "Admin TwentyOne", "Owner");
            admin.SelectedRestaurantId = restaurant.Id;
            var billingAdmin = AddUser(context, "Billing Admin TwentyOne", "Owner");
            billingAdmin.SelectedRestaurantId = restaurant.Id;
            var staff = AddUser(context, "Staff TwentyOne", "Owner");
            staff.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            AddMembership(
                context,
                admin.Id,
                restaurant.Id,
                PermissionRoles.Admin,
                LocationScopeKind.AllLocations,
                "[]"
            );
            AddMembership(
                context,
                billingAdmin.Id,
                restaurant.Id,
                PermissionRoles.BillingAdmin,
                LocationScopeKind.AllLocations,
                "[]"
            );
            AddMembership(
                context,
                staff.Id,
                restaurant.Id,
                PermissionRoles.Staff,
                LocationScopeKind.NamedList,
                MembershipLocationScope.SerializeNamedIds([location.Id])
            );
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                owner.Id,
                admin.Id,
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                jwtService.GenerateToken(admin.Id.ToString(), admin.Email, admin.Role),
                jwtService.GenerateToken(
                    billingAdmin.Id.ToString(),
                    billingAdmin.Email,
                    billingAdmin.Role
                ),
                jwtService.GenerateToken(staff.Id.ToString(), staff.Email, staff.Role)
            );
        }

        private static User AddUser(
            ApplicationDbContext context,
            string name,
            string role
        )
        {
            var user = new User
            {
                FullName = name,
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = role,
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            return user;
        }

        private static RestaurantMembership AddMembership(
            ApplicationDbContext context,
            int userId,
            int restaurantId,
            string permissionRole,
            LocationScopeKind scope,
            string namedJson
        )
        {
            var row = new RestaurantMembership
            {
                UserId = userId,
                RestaurantId = restaurantId,
                PermissionRole = permissionRole,
                LocationScope = scope,
                NamedLocationIdsJson = namedJson,
                Status = MembershipStatus.Active,
            };
            context.RestaurantMemberships.Add(row);
            return row;
        }

        private static HttpRequestMessage Authorized(
            HttpMethod method,
            string url,
            string jwt
        )
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedPutBillingContacts(
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/billing-credits/billing-contacts"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(payload);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record Seeded(
            int RestaurantId,
            int OwnerUserId,
            int AdminUserId,
            string OwnerJwt,
            string AdminJwt,
            string BillingAdminJwt,
            string StaffJwt
        );
    }
}
