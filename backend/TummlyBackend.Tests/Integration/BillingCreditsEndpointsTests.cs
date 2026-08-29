using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Controllers;
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
        public async Task BillingCredits_HasNoOperatorRefundRoute()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            var refundPaths = new[]
            {
                "/api/billing-credits/refund",
                "/api/billing-credits/top-up/refund",
                "/api/billing-credits/credits/refund",
            };

            foreach (var path in refundPaths)
            {
                using var postRequest = new HttpRequestMessage(HttpMethod.Post, path);
                postRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
                postRequest.Content = JsonContent.Create(new { channel = "email", quantity = 1 });
                var postResponse = await _client.SendAsync(postRequest);
                Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);

                using var getRequest = Authorized(
                    HttpMethod.Get,
                    path,
                    seeded.OwnerJwt
                );
                var getResponse = await _client.SendAsync(getRequest);
                Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
            }

            var controllerMethods = typeof(BillingCreditsController)
                .GetMethods()
                .Where(method => method.IsPublic && method.DeclaringType == typeof(BillingCreditsController))
                .Select(method => method.Name)
                .ToArray();
            Assert.DoesNotContain(controllerMethods, name =>
                name.Contains("Refund", StringComparison.OrdinalIgnoreCase)
            );
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
            Assert.Equal(
                "TUMMLY-UK-GBP-2026-08-V3",
                plan.GetProperty("pricebookId").GetString()
            );
            Assert.Equal("500 once", plan.GetProperty("includedEmailCreditsLabel").GetString());
            Assert.Equal("20 once", plan.GetProperty("includedSmsCreditsLabel").GetString());
            Assert.Equal("20 once", plan.GetProperty("includedAiCreditsLabel").GetString());
            Assert.Equal("£0", plan.GetProperty("planPriceNet").GetString());
            Assert.Equal("unused", plan.GetProperty("starterKitState").GetString());
            Assert.Equal(0, plan.GetProperty("emailCreditsRemaining").GetInt32());
            Assert.Equal(0, plan.GetProperty("smsCreditsRemaining").GetInt32());
            Assert.Equal(0, plan.GetProperty("aiCreditsRemaining").GetInt32());
            Assert.True(plan.GetProperty("isPilot").GetBoolean());
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("paymentMethod").ValueKind
            );
            Assert.Equal(0, body.GetProperty("invoices").GetArrayLength());
            Assert.Equal("view", body.GetProperty("accessLevel").GetString());
            Assert.False(body.GetProperty("writeCapabilities").GetProperty("changePlan").GetBoolean());
            Assert.False(body.GetProperty("writeCapabilities").GetProperty("buyTopup").GetBoolean());

            var catalog = body.GetProperty("currentCatalog");
            Assert.Equal(
                "TUMMLY-UK-GBP-2026-08-V3",
                catalog.GetProperty("pricebookId").GetString()
            );
            Assert.Equal(2000, catalog.GetProperty("vatRateBps").GetInt32());
            Assert.False(catalog.GetProperty("sms5000Available").GetBoolean());
            Assert.True(catalog.GetProperty("plans").GetArrayLength() >= 3);

            var contacts = body.GetProperty("billingContacts");
            Assert.True(contacts.GetProperty("lowCreditAlerts").GetProperty("owner").GetBoolean());
            Assert.False(contacts.GetProperty("lowCreditAlerts").GetProperty("admin").GetBoolean());
            Assert.True(
                contacts.GetProperty("lowCreditAlerts").GetProperty("billingContact").GetBoolean()
            );
        }

        [Fact]
        public async Task SetupAccount_CreatesBillingAccountRow_WithPilotDefaults()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                context.TrialRequests.Add(
                    new TrialRequest
                    {
                        BusinessName = "Setup Billing Cafe",
                        BusinessCategory = "takeaway",
                        Locations = "1",
                        FullName = "Setup Owner",
                        Email = "setup-billing@example.com",
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
                        ApprovalToken = "setup-billing-token",
                        InviteExpiresAt = DateTime.UtcNow.AddDays(7),
                        IsAccountCreated = false,
                        AccountType = "Single",
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            var response = await _client.PostAsJsonAsync(
                "/api/auth/setup-account",
                new
                {
                    token = "setup-billing-token",
                    password = "Password1!",
                    confirmPassword = "Password1!",
                    fullName = "Setup Owner",
                    groupName = "Setup Billing Cafe",
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurant = await context.Restaurants
                    .AsNoTracking()
                    .SingleAsync(row => row.Name == "Setup Billing Cafe");
                var billingAccount = await context.BillingAccounts
                    .AsNoTracking()
                    .SingleAsync(row => row.RestaurantId == restaurant.Id);

                Assert.Equal(restaurant.Id, billingAccount.RestaurantId);
                Assert.Equal(BillingSubscriptionPlans.Pilot, billingAccount.SubscriptionPlan);
                Assert.Equal(BillingStatuses.Pilot, billingAccount.BillingStatus);
                Assert.Null(billingAccount.BillingCycle);
                Assert.Null(billingAccount.RevolutCustomerId);
                Assert.Equal(StarterKitStates.Unused, billingAccount.StarterKitState);
                Assert.Equal(
                    "TUMMLY-UK-GBP-2026-08-V3",
                    billingAccount.ContractedPricebookId
                );

                var ledgerRows = await context.CreditLedgerEntries
                    .AsNoTracking()
                    .Where(row => row.RestaurantId == restaurant.Id)
                    .ToListAsync();
                Assert.Empty(ledgerRows);
            }
        }

        [Fact]
        public async Task ActivateThenUsageGet_MatchesOneTimePilotAllowances()
        {
            var seeded = await SeedPendingActivationWorkspaceAsync();

            using (var activateRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/auth/activate"
            ))
            {
                activateRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
                activateRequest.Content = JsonContent.Create(new
                {
                    activationCode = "ABCD-2345",
                });

                var activateResponse = await _client.SendAsync(activateRequest);
                Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
            }

            using var usageRequest = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/usage",
                seeded.OwnerJwt
            );
            var usageResponse = await _client.SendAsync(usageRequest);
            Assert.Equal(HttpStatusCode.OK, usageResponse.StatusCode);

            var body = await ReadJsonAsync(usageResponse);
            Assert.Equal("Account · Pilot allowance", body.GetProperty("periodLabel").GetString());
            Assert.True(body.GetProperty("isPilot").GetBoolean());

            var email = body.GetProperty("channels").EnumerateArray()
                .First(row => row.GetProperty("channel").GetString() == "email");
            var sms = body.GetProperty("channels").EnumerateArray()
                .First(row => row.GetProperty("channel").GetString() == "sms");
            var ai = body.GetProperty("channels").EnumerateArray()
                .First(row => row.GetProperty("channel").GetString() == "ai");

            Assert.Equal(500, email.GetProperty("combinedRemaining").GetInt32());
            Assert.Equal(500, email.GetProperty("includedThisPeriod").GetInt32());
            Assert.Equal(20, sms.GetProperty("combinedRemaining").GetInt32());
            Assert.Equal(20, sms.GetProperty("includedThisPeriod").GetInt32());
            Assert.Equal(20, ai.GetProperty("combinedRemaining").GetInt32());
            Assert.Equal(20, ai.GetProperty("includedThisPeriod").GetInt32());
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


        [Fact]
        public async Task UsageGet_Returns401_WithoutJwt()
        {
            var response = await _client.GetAsync("/api/billing-credits/usage");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UsageGet_Returns403_ForStaff()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/usage",
                seeded.StaffJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UsageGet_ReturnsPilotSnapshot_ForAdminView()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/usage",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("Account · Pilot allowance", body.GetProperty("periodLabel").GetString());
            Assert.True(body.GetProperty("isPilot").GetBoolean());
            Assert.Equal("unused", body.GetProperty("starterKitState").GetString());

            var channels = body.GetProperty("channels");
            Assert.Equal(3, channels.GetArrayLength());
            foreach (var channel in channels.EnumerateArray())
            {
                Assert.Equal(0, channel.GetProperty("combinedRemaining").GetInt32());
                Assert.Equal(0, channel.GetProperty("held").GetInt32());
                Assert.Equal(0, channel.GetProperty("usedThisCycle").GetInt32());
                Assert.Equal(0, channel.GetProperty("includedThisPeriod").GetInt32());
                Assert.Equal(1m, channel.GetProperty("usedShare").GetDecimal());
            }
        }

        [Fact]
        public async Task UsageGet_ReturnsPilotSnapshot_ForOwnerManage()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/usage",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var sms = body.GetProperty("channels").EnumerateArray()
                .First(row => row.GetProperty("channel").GetString() == "sms");
            Assert.Equal(0, sms.GetProperty("combinedRemaining").GetInt32());
            Assert.Equal(0, sms.GetProperty("includedThisPeriod").GetInt32());
            Assert.Equal(0, sms.GetProperty("held").GetInt32());
            Assert.Equal(1m, sms.GetProperty("usedShare").GetDecimal());
        }

        [Fact]
        public async Task UsageGet_ReturnsPilotSnapshot_ForMarketingView()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/usage",
                seeded.MarketingJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(3, body.GetProperty("channels").GetArrayLength());
        }

        [Fact]
        public async Task PostPlanChange_Returns403_ForAdminView()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/plan-change",
                seeded.AdminJwt
            );
            request.Content = JsonContent.Create(new
            {
                targetPlan = "Starter",
                targetCadence = "monthly",
            });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostPlanChange_Returns403_ForBillingAdminManage()
        {
            var seeded = await SeedWorkspaceAsync(includeBillingAdmin: true);
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/plan-change",
                seeded.BillingAdminJwt
            );
            request.Content = JsonContent.Create(new
            {
                targetPlan = "Starter",
                targetCadence = "monthly",
            });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostPlanChange_ReturnsPayRedirect_ForOwnerPilotConversion()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/plan-change",
                seeded.OwnerJwt
            );
            request.Content = JsonContent.Create(new
            {
                targetPlan = "Starter",
                targetCadence = "monthly",
            });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("pay", body.GetProperty("outcome").GetString());
            Assert.Contains(
                "checkout.revolut.com",
                body.GetProperty("redirectUrl").GetString()
            );
        }

        [Fact]
        public async Task PostPlanChange_ReturnsPayRedirect_ForOwnerAnnualPilotConversion()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/plan-change",
                seeded.OwnerJwt
            );
            request.Content = JsonContent.Create(new
            {
                targetPlan = "Starter",
                targetCadence = "annual",
            });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("pay", body.GetProperty("outcome").GetString());
        }

        [Fact]
        public async Task PostPlanChange_ReturnsScheduled_ForOwnerPaidDowngrade()
        {
            var seeded = await SeedWorkspaceAsync(scheduleTestRestaurant: true);
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/plan-change",
                seeded.OwnerJwt
            );
            request.Content = JsonContent.Create(new
            {
                targetPlan = "Starter",
                targetCadence = "monthly",
            });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("scheduled", body.GetProperty("outcome").GetString());
            Assert.Contains(
                "Changes to Starter on",
                body.GetProperty("scheduledChangeLine").GetString()
            );
        }

        [Fact]
        public async Task PostExtraLocationAdd_Returns403_ForAdminView()
        {
            var seeded = await SeedGroupWorkspaceAsync();
            using var request = AuthorizedExtraLocation(
                seeded.AdminJwt,
                "add",
                withIdempotencyKey: true
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostExtraLocationAdd_Returns403_ForNonOwner()
        {
            var seeded = await SeedGroupWorkspaceAsync();
            using var request = AuthorizedExtraLocation(
                seeded.BillingAdminJwt,
                "add",
                withIdempotencyKey: true
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "billing_write_not_permitted",
                body.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task PostExtraLocationAdd_ReturnsPayRedirect_ForOwnerGroup()
        {
            var seeded = await SeedGroupWorkspaceAsync();
            using var request = AuthorizedExtraLocation(
                seeded.OwnerJwt,
                "add",
                withIdempotencyKey: true
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("pay", body.GetProperty("outcome").GetString());
            Assert.Contains(
                "checkout.revolut.com",
                body.GetProperty("redirectUrl").GetString()
            );
        }

        [Fact]
        public async Task PostExtraLocationAdd_Returns400_WhenNotGroup()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = AuthorizedExtraLocation(
                seeded.OwnerJwt,
                "add",
                withIdempotencyKey: true
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "extra_location_not_group",
                body.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task PostExtraLocationAdd_Returns409_AtSelfServeMax()
        {
            var seeded = await SeedGroupWorkspaceAsync(paidExtraLocationCount: 25);
            using var request = AuthorizedExtraLocation(
                seeded.OwnerJwt,
                "add",
                withIdempotencyKey: true
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "group_location_self_serve_max_reached",
                body.GetProperty("code").GetString()
            );
            Assert.Equal(30, body.GetProperty("cap").GetInt32());
            Assert.Equal(30, body.GetProperty("current").GetInt32());
        }

        [Fact]
        public async Task PostExtraLocationRemove_Returns403_ForAdminView()
        {
            var seeded = await SeedGroupWorkspaceAsync(paidExtraLocationCount: 2);
            using var request = AuthorizedExtraLocation(
                seeded.AdminJwt,
                "remove",
                withIdempotencyKey: false
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostExtraLocationRemove_Returns403_ForNonOwner()
        {
            var seeded = await SeedGroupWorkspaceAsync(paidExtraLocationCount: 2);
            using var request = AuthorizedExtraLocation(
                seeded.BillingAdminJwt,
                "remove",
                withIdempotencyKey: false
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostExtraLocationRemove_Schedules_ForOwnerGroup()
        {
            var seeded = await SeedGroupWorkspaceAsync(paidExtraLocationCount: 2);
            using var request = AuthorizedExtraLocation(
                seeded.OwnerJwt,
                "remove",
                withIdempotencyKey: false
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("scheduled", body.GetProperty("outcome").GetString());
            Assert.Contains(
                "Removes 1 Additional Group Location",
                body.GetProperty("scheduledChangeLine").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await context.BillingAccounts.SingleAsync(row =>
                row.RestaurantId == seeded.RestaurantId
            );
            Assert.True(account.HasScheduledChange);
            Assert.Equal(1, account.ScheduledTargetExtraLocationCount);
            Assert.Equal(2, account.PaidExtraLocationCount);
        }

        [Fact]
        public async Task PostExtraLocationRemove_RejectsBelowFloor_ForOwnerGroup()
        {
            var seeded = await SeedGroupWorkspaceAsync();
            using var request = AuthorizedExtraLocation(
                seeded.OwnerJwt,
                "remove",
                withIdempotencyKey: false
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostCancelPlan_Returns403_ForAdminView()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/cancel-plan",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostCancelPlan_Returns403_ForBillingAdminManage()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/cancel-plan",
                seeded.BillingAdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostCancelPlan_RejectsPilot_ForOwner()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/cancel-plan",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostCancelPlan_ReturnsScheduledLine_ForOwnerPaid()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/cancel-plan",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.StartsWith(
                "Cancels on",
                body.GetProperty("scheduledChangeLine").GetString()
            );
        }


        private const string CurrentPricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private static BillingAccount CreateSeedBillingAccount(
            int restaurantId,
            string restaurantName,
            bool allowSms5000TopUp = false
        )
        {
            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurantId,
                CurrentPricebookId
            );
            account.AllowSms5000TopUp = allowSms5000TopUp;

            var lifecycle = BillingPlanSnapshotHelper.ResolveLifecycle(
                restaurantName,
                activationExpiresAt: DateTime.UtcNow.AddDays(30)
            );
            account.SubscriptionPlan = lifecycle.SubscriptionPlan;
            account.BillingStatus = lifecycle.BillingStatus;
            if (!lifecycle.IsPilot)
            {
                account.BillingCycle = BillingCycles.Monthly;
            }

            return account;
        }

        private async Task<Seeded> SeedGroupWorkspaceAsync(
            int paidExtraLocationCount = 0
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = AddUser(context, "Owner Group", "Owner");
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Paid Billing Venue Group Test",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var account = CreateSeedBillingAccount(restaurant.Id, restaurant.Name);
            account.PaidExtraLocationCount = paidExtraLocationCount;
            context.BillingAccounts.Add(account);

            owner.SelectedRestaurantId = restaurant.Id;

            for (var i = 0; i < 5; i++)
            {
                context.RestaurantLocations.Add(new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = $"Location {i + 1}",
                    Address = $"{i + 1} High Street",
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await context.SaveChangesAsync();

            AddMembership(
                context,
                owner.Id,
                restaurant.Id,
                PermissionRoles.Owner,
                LocationScopeKind.AllLocations,
                "[]"
            );

            var admin = AddUser(context, "Admin Group", "Owner");
            admin.SelectedRestaurantId = restaurant.Id;
            var staff = AddUser(context, "Staff Group", "Owner");
            staff.SelectedRestaurantId = restaurant.Id;
            var marketing = AddUser(context, "Marketing Group", "Owner");
            marketing.SelectedRestaurantId = restaurant.Id;
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
                staff.Id,
                restaurant.Id,
                PermissionRoles.Staff,
                LocationScopeKind.AllLocations,
                "[]"
            );
            AddMembership(
                context,
                marketing.Id,
                restaurant.Id,
                PermissionRoles.Marketing,
                LocationScopeKind.AllLocations,
                "[]"
            );

            var billingAdmin = AddUser(context, "Billing Admin Group", "Owner");
            billingAdmin.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            AddMembership(
                context,
                billingAdmin.Id,
                restaurant.Id,
                PermissionRoles.BillingAdmin,
                LocationScopeKind.AllLocations,
                "[]"
            );

            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                owner.Id,
                admin.Id,
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                jwtService.GenerateToken(admin.Id.ToString(), admin.Email, admin.Role),
                jwtService.GenerateToken(staff.Id.ToString(), staff.Email, staff.Role),
                jwtService.GenerateToken(
                    marketing.Id.ToString(),
                    marketing.Email,
                    marketing.Role
                ),
                jwtService.GenerateToken(
                    billingAdmin.Id.ToString(),
                    billingAdmin.Email,
                    billingAdmin.Role
                )
            );
        }

        private static HttpRequestMessage AuthorizedExtraLocation(
            string jwt,
            string action,
            bool withIdempotencyKey
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/billing-credits/extra-location"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            if (withIdempotencyKey)
            {
                request.Headers.TryAddWithoutValidation(
                    "Idempotency-Key",
                    Guid.NewGuid().ToString("N")
                );
            }

            request.Content = JsonContent.Create(new { action });
            return request;
        }

        [Fact]
        public async Task GetActivity_Returns401_WithoutJwt()
        {
            var response = await _client.GetAsync("/api/billing-credits/activity");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_Returns403_ForNoAccess()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/activity",
                seeded.StaffJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetActivity_ReturnsEmpty_ForView()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/activity?skip=0&take=10",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(0, body.GetProperty("items").GetArrayLength());
            Assert.Equal(0, body.GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task GetActivity_CapsTakeAtTwenty()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                for (var i = 0; i < 25; i++)
                {
                    context.RestaurantBillingActivities.Add(
                        new RestaurantBillingActivity
                        {
                            RestaurantId = seeded.RestaurantId,
                            Kind = BillingActivityKinds.SubscriptionRenewed,
                            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-i),
                            Plan = "Growth",
                        }
                    );
                }

                await context.SaveChangesAsync();
            }

            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/activity?skip=0&take=50",
                seeded.OwnerJwt
            );
            var body = await ReadJsonAsync(await _client.SendAsync(request));
            Assert.Equal(20, body.GetProperty("items").GetArrayLength());
            Assert.Equal(25, body.GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task GetActivity_ReturnsNewestFirst_AndSentenceMatchesFrontend08Copy()
        {
            var seeded = await SeedPaidWorkspaceAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurantId = seeded.RestaurantId;
                context.RestaurantAccessActivities.Add(
                    new RestaurantAccessActivity
                    {
                        RestaurantId = restaurantId,
                        ActorUserId = seeded.OwnerUserId,
                        ActorDisplayName = "Owner Paid",
                        Kind = AccessActivityKinds.MemberRemoved,
                        OccurredAt = DateTime.UtcNow,
                        TargetDisplayName = "Removed Person",
                    }
                );
                context.RestaurantBillingActivities.AddRange(
                    new RestaurantBillingActivity
                    {
                        RestaurantId = restaurantId,
                        Kind = BillingActivityKinds.CreditConsumed,
                        OccurredAtUtc = DateTime.UtcNow.AddHours(-1),
                        Channel = CreditChannels.Sms,
                        Qty = 212,
                        CampaignName = "Quiet Tuesday Boost",
                        ConsumeSource = "campaign",
                    },
                    new RestaurantBillingActivity
                    {
                        RestaurantId = restaurantId,
                        Kind = BillingActivityKinds.TopupPurchased,
                        OccurredAtUtc = DateTime.UtcNow.AddHours(-2),
                        ActorDisplayName = "James Cole",
                        Channel = CreditChannels.Sms,
                        Qty = 1000,
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/activity?skip=0&take=10",
                seeded.AdminJwt
            );
            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var items = body.GetProperty("items");
            Assert.Equal(2, items.GetArrayLength());
            Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                BillingActivityKinds.CreditConsumed,
                items[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "212 SMS credits used by Quiet Tuesday Boost.",
                items[0].GetProperty("sentence").GetString()
            );
            Assert.Equal(
                "1,000 SMS credits added by James Cole.",
                items[1].GetProperty("sentence").GetString()
            );
            Assert.All(
                items.EnumerateArray(),
                row => Assert.DoesNotContain(
                    row.GetProperty("kind").GetString() ?? "",
                    new[]
                    {
                        AccessActivityKinds.MemberRemoved,
                        AccessActivityKinds.RoleChanged,
                        AccessActivityKinds.PermissionCellChanged,
                    }
                )
            );
        }

        private async Task<Seeded> SeedWorkspaceAsync(
            bool includeBillingAdmin = false,
            bool scheduleTestRestaurant = false,
            DateTime? activationExpiresAt = null,
            string? restaurantName = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = AddUser(context, "Owner Fifteen", "Owner");
            if (activationExpiresAt != null)
            {
                owner.ActivationExpiresAt = activationExpiresAt;
            }
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = restaurantName
                    ?? (scheduleTestRestaurant
                        ? "Billing Venue Schedule Test"
                        : "Billing Venue"),
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
                CreateSeedBillingAccount(restaurant.Id, restaurant.Name)
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

            var admin = AddUser(context, "Admin Fifteen", "Owner");
            admin.SelectedRestaurantId = restaurant.Id;
            var staff = AddUser(context, "Staff Fifteen", "Owner");
            staff.SelectedRestaurantId = restaurant.Id;
            var marketing = AddUser(context, "Marketing Eighteen", "Owner");
            marketing.SelectedRestaurantId = restaurant.Id;
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
                staff.Id,
                restaurant.Id,
                PermissionRoles.Staff,
                LocationScopeKind.NamedList,
                MembershipLocationScope.SerializeNamedIds([location.Id])
            );
            AddMembership(
                context,
                marketing.Id,
                restaurant.Id,
                PermissionRoles.Marketing,
                LocationScopeKind.AllLocations,
                "[]"
            );

            var billingAdmin = AddUser(context, "Billing Admin Sixteen", "Owner");
            billingAdmin.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            AddMembership(
                context,
                billingAdmin.Id,
                restaurant.Id,
                PermissionRoles.BillingAdmin,
                LocationScopeKind.AllLocations,
                "[]"
            );

            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                owner.Id,
                admin.Id,
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                jwtService.GenerateToken(admin.Id.ToString(), admin.Email, admin.Role),
                jwtService.GenerateToken(staff.Id.ToString(), staff.Email, staff.Role),
                jwtService.GenerateToken(
                    marketing.Id.ToString(),
                    marketing.Email,
                    marketing.Role
                ),
                jwtService.GenerateToken(
                    billingAdmin.Id.ToString(),
                    billingAdmin.Email,
                    billingAdmin.Role
                )
            );
        }

        [Fact]
        public async Task Get_ReturnsInvoices_ForPaidAccountView()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var invoices = body.GetProperty("invoices");
            Assert.Equal(3, invoices.GetArrayLength());
            Assert.Equal(
                "TM-2026-000001",
                invoices[0].GetProperty("invoiceNo").GetString()
            );
        }

        [Fact]
        public async Task GetInvoicePdf_ReturnsPdf_ForView()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits/invoices/TM-2026-000001/pdf",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "application/pdf",
                response.Content.Headers.ContentType?.MediaType
            );
        }

        [Fact]
        public async Task PostPaymentMethodUpdate_Returns403_ForView()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/payment-method/update",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostPaymentMethodUpdate_ReturnsRedirect_ForOwner()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/payment-method/update",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Contains(
                "revolut.com",
                body.GetProperty("redirectUrl").GetString()
            );
        }

        [Fact]
        public async Task PostPaymentMethodUpdate_ReturnsRedirect_ForBillingAdmin()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Post,
                "/api/billing-credits/payment-method/update",
                seeded.BillingAdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PostTopUpConfirm_Returns403_ForView()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = AuthorizedTopUpConfirm(
                seeded.AdminJwt,
                new { channel = "sms", quantity = 100 }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostTopUpPay_Returns403_ForView()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using var request = AuthorizedTopUpPay(
                seeded.AdminJwt,
                new { channel = "sms", quantity = 100 }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostTopUpConfirm_Returns403_ForPilot()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedTopUpConfirm(
                seeded.OwnerJwt,
                new { channel = "sms", quantity = 100 }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Contains(
                "Pilot",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task PostTopUpPay_Returns403_ForPilot()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedTopUpPay(
                seeded.OwnerJwt,
                new { channel = "sms", quantity = 100 }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostTopUpConfirm_ReturnsOk_ForSms5000_OnStarterWithApprovalFlag()
        {
            var seeded = await SeedPaidStarterWorkspaceAsync(allowSms5000TopUp: true);
            using var request = AuthorizedTopUpConfirm(
                seeded.OwnerJwt,
                new { channel = "sms", quantity = 5000 }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(5000, body.GetProperty("quantity").GetInt32());
        }

        [Fact]
        public async Task PostTopUpConfirm_Returns403_ForSms5000_OnStarterWithoutApproval()
        {
            var seeded = await SeedPaidStarterWorkspaceAsync();
            using var request = AuthorizedTopUpConfirm(
                seeded.OwnerJwt,
                new { channel = "sms", quantity = 5000 }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostTopUpConfirm_ReturnsOk_ForSms5000_OnGroup()
        {
            var seeded = await SeedPaidGroupWorkspaceAsync();
            using var request = AuthorizedTopUpConfirm(
                seeded.OwnerJwt,
                new { channel = "sms", quantity = 5000 }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("sms", body.GetProperty("channel").GetString());
            Assert.Equal(5000, body.GetProperty("quantity").GetInt32());
            Assert.Equal("£450", body.GetProperty("netLabel").GetString());
            Assert.Equal("£540", body.GetProperty("grossLabel").GetString());
        }

        [Fact]
        public async Task Get_Returns200_SoftLock_ForAdminView()
        {
            var seeded = await SeedSoftLockPilotWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("actorCanManage").GetBoolean());
            var plan = body.GetProperty("planSubscription");
            Assert.Equal("Pilot", plan.GetProperty("subscriptionPlan").GetString());
            Assert.Equal("Soft lock", plan.GetProperty("billingStatus").GetString());
            Assert.True(plan.GetProperty("isPilot").GetBoolean());
        }

        [Fact]
        public async Task Get_AfterPilotPeriodEnd_ShowsSoftLock()
        {
            var seeded = await SeedWorkspaceAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var account = await context.BillingAccounts.SingleAsync(
                    row => row.RestaurantId == seeded.RestaurantId
                );
                account.BillingStatus = BillingStatuses.Pilot;
                account.PilotPeriodEnd = DateTime.UtcNow.AddHours(-1);
                var owner = await context.Users.SingleAsync(row => row.Id == seeded.OwnerUserId);
                owner.ActivatedAt = DateTime.UtcNow.AddDays(-31);
                owner.ActivationExpiresAt = account.PilotPeriodEnd;
                await context.SaveChangesAsync();
            }

            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var plan = (await ReadJsonAsync(response)).GetProperty("planSubscription");
            Assert.Equal("Soft lock", plan.GetProperty("billingStatus").GetString());
        }

        [Fact]
        public async Task Get_Returns200_SoftLock_ForOwnerManage()
        {
            var seeded = await SeedSoftLockPilotWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("actorCanManage").GetBoolean());
            Assert.Equal(
                "Soft lock",
                body.GetProperty("planSubscription").GetProperty("billingStatus").GetString()
            );
        }

        [Fact]
        public async Task Get_Returns200_Dormant_ForOwnerManage()
        {
            var seeded = await SeedDormantPilotWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var plan = (await ReadJsonAsync(response)).GetProperty("planSubscription");
            Assert.Equal("Dormant", plan.GetProperty("billingStatus").GetString());
            Assert.True(plan.GetProperty("isPilot").GetBoolean());
        }

        [Fact]
        public async Task Get_Returns200_Dormant_ForAdminView()
        {
            var seeded = await SeedDormantPilotWorkspaceAsync();
            using var request = Authorized(
                HttpMethod.Get,
                "/api/billing-credits",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("actorCanManage").GetBoolean());
            Assert.Equal(
                "Dormant",
                body.GetProperty("planSubscription").GetProperty("billingStatus").GetString()
            );
        }

        [Fact]
        public async Task PostTopUpConfirm_Returns403_SoftLockPaid()
        {
            var seeded = await SeedNamedPaidWorkspaceAsync("Soft lock Paid Growth Venue");
            using var request = AuthorizedTopUpConfirm(
                seeded.OwnerJwt,
                new { channel = "sms", quantity = 100 }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("soft_lock", body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task PostPlanChange_Returns403_SoftLockPaid_NonRestoration()
        {
            var seeded = await SeedNamedPaidWorkspaceAsync("Soft lock Paid Growth Venue");
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/billing-credits/plan-change"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                seeded.OwnerJwt
            );
            request.Content = JsonContent.Create(
                new { targetPlan = "Group", targetCadence = "monthly" }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostPlanChange_AllowsUnpaidPilotRestoration_DuringSoftLock()
        {
            var seeded = await SeedSoftLockPilotWorkspaceAsync();
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/billing-credits/plan-change"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                seeded.OwnerJwt
            );
            request.Content = JsonContent.Create(
                new { targetPlan = "Starter", targetCadence = "monthly" }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("pay", body.GetProperty("outcome").GetString());
        }

        private Task<Seeded> SeedSoftLockPilotWorkspaceAsync()
        {
            return SeedWorkspaceAsync(restaurantName: "Soft lock Billing Venue");
        }

        private Task<Seeded> SeedDormantPilotWorkspaceAsync()
        {
            return SeedWorkspaceAsync(restaurantName: "Dormant Billing Venue");
        }

        [Fact]
        public async Task PostTopUpConfirmThenPay_ReturnsRevolutRedirect_ForOwner()
        {
            var seeded = await SeedPaidWorkspaceAsync();
            using (var confirmRequest = AuthorizedTopUpConfirm(
                seeded.OwnerJwt,
                new { channel = "ai", quantity = 500 }
            ))
            {
                var confirmResponse = await _client.SendAsync(confirmRequest);
                Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
                var confirmBody = await ReadJsonAsync(confirmResponse);
                Assert.Equal("£15", confirmBody.GetProperty("netLabel").GetString());
                Assert.Equal("£18", confirmBody.GetProperty("grossLabel").GetString());
            }

            using var payRequest = AuthorizedTopUpPay(
                seeded.OwnerJwt,
                new { channel = "ai", quantity = 500 }
            );
            var payResponse = await _client.SendAsync(payRequest);
            Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);

            var payBody = await ReadJsonAsync(payResponse);
            Assert.Contains(
                "checkout.revolut.com",
                payBody.GetProperty("redirectUrl").GetString()
            );
        }

        private async Task<PaidSeeded> SeedPaidStarterWorkspaceAsync(
            bool allowSms5000TopUp = false
        )
        {
            return await SeedNamedPaidWorkspaceAsync(
                "Paid Starter Venue",
                allowSms5000TopUp
            );
        }

        private async Task<PaidSeeded> SeedPaidGroupWorkspaceAsync()
        {
            return await SeedNamedPaidWorkspaceAsync("Paid Group Venue");
        }

        private async Task<PaidSeeded> SeedNamedPaidWorkspaceAsync(
            string venueName,
            bool allowSms5000TopUp = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = AddUser(context, $"Owner {venueName}", "Owner");
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = venueName,
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
                CreateSeedBillingAccount(restaurant.Id, venueName, allowSms5000TopUp)
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
            await context.SaveChangesAsync();

            return new PaidSeeded(
                restaurant.Id,
                owner.Id,
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                string.Empty,
                string.Empty
            );
        }

        private static HttpRequestMessage AuthorizedTopUpConfirm(
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/billing-credits/top-up/confirm"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(payload);
            return request;
        }

        private static HttpRequestMessage AuthorizedTopUpPay(
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/billing-credits/top-up/pay"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(payload);
            return request;
        }

        private async Task<PaidSeeded> SeedPaidWorkspaceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = AddUser(context, "Owner Paid", "Owner");
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Paid Billing Venue",
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
                CreateSeedBillingAccount(restaurant.Id, restaurant.Name)
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

            var admin = AddUser(context, "Admin Paid", "Owner");
            admin.SelectedRestaurantId = restaurant.Id;
            var billingAdmin = AddUser(context, "Billing Admin Paid", "Owner");
            billingAdmin.SelectedRestaurantId = restaurant.Id;
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
            await context.SaveChangesAsync();

            return new PaidSeeded(
                restaurant.Id,
                owner.Id,
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                jwtService.GenerateToken(admin.Id.ToString(), admin.Email, admin.Role),
                jwtService.GenerateToken(
                    billingAdmin.Id.ToString(),
                    billingAdmin.Email,
                    billingAdmin.Role
                )
            );
        }

        private async Task<PendingActivationSeeded> SeedPendingActivationWorkspaceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Pending Activation Owner",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900222",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                CreatedAt = DateTime.UtcNow,
                ActivationCodeHash =
                    ActivationCodeHelper.HashCode("ABCD2345"),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Pending Activation Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                CreateSeedBillingAccount(restaurant.Id, restaurant.Name)
            );
            owner.SelectedRestaurantId = restaurant.Id;

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            AddMembership(
                context,
                owner.Id,
                restaurant.Id,
                PermissionRoles.Owner,
                LocationScopeKind.AllLocations,
                "[]"
            );
            await context.SaveChangesAsync();

            return new PendingActivationSeeded(
                restaurant.Id,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                )
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
            string StaffJwt,
            string MarketingJwt,
            string BillingAdminJwt
        );

        private sealed record PendingActivationSeeded(
            int RestaurantId,
            string OwnerJwt
        );

        private sealed record PaidSeeded(
            int RestaurantId,
            int OwnerUserId,
            string OwnerJwt,
            string AdminJwt,
            string BillingAdminJwt
        );
    }
}
