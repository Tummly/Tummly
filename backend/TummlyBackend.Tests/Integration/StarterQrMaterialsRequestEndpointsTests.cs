using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public sealed class StarterQrMaterialsRequestEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public StarterQrMaterialsRequestEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
        }

        [Fact]
        public async Task SetupAccount_RequestsEveryLocation_AndActivationCodeDoesNotWaitForSlowPrint()
        {
            var releasePrint = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var printWork = new ControllablePrintReadyQrMaterialsWork(
                _ => releasePrint.Task
            );
            await using var factory = CreateFactory(printWork);
            using var client = factory.CreateClient();
            const string token = "starter-materials-multi-token";

            await SeedInviteAsync(
                factory.Services,
                "starter-materials-multi@example.com",
                token,
                "Starter Materials Group",
                "Multi"
            );

            var setupTask = client.PostAsJsonAsync(
                "/api/auth/setup-account",
                new
                {
                    token,
                    password = "Password1!",
                    confirmPassword = "Password1!",
                    fullName = "Starter Owner",
                    groupName = "Starter Materials Group",
                    businessCategory = "multi-site",
                    primaryPhone = "07911123456",
                    locations = new[]
                    {
                        new
                        {
                            locationName = "Leeds",
                            address = "1 High Street",
                            city = "Leeds",
                            postcode = "LS1 1AA",
                        },
                        new
                        {
                            locationName = "York",
                            address = "2 Stonegate",
                            city = "York",
                            postcode = "YO1 8AS",
                        },
                    },
                }
            );

            using var setupResponse = await setupTask.WaitAsync(
                TimeSpan.FromSeconds(5)
            );
            Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);

            var requestedLocationIds = new[]
            {
                await printWork.NextRequestAsync(),
                await printWork.NextRequestAsync(),
            };

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var createdLocationIds = await context.RestaurantLocations
                    .Where(row =>
                        row.Restaurant!.Name == "Starter Materials Group"
                    )
                    .OrderBy(row => row.Id)
                    .Select(row => row.Id)
                    .ToArrayAsync();

                Assert.Equal(
                    createdLocationIds,
                    requestedLocationIds.Order().ToArray()
                );
            }

            Assert.Equal(0, printWork.CompletedCount);

            using var activationResponse = await client
                .PostAsJsonAsync(
                    "/api/auth/generate-activation-code",
                    new { token }
                )
                .WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(HttpStatusCode.OK, activationResponse.StatusCode);
            Assert.Equal(0, printWork.CompletedCount);

            releasePrint.SetResult();
            await printWork.WaitForCompletedAsync(2);
        }

        [Fact]
        public async Task AddOwnedLocation_MintsDefaults_AndSucceedsWhenPrintFails()
        {
            var printWork = new ControllablePrintReadyQrMaterialsWork(
                _ => Task.FromException(
                    new InvalidOperationException("Controlled print failure.")
                )
            );
            await using var factory = CreateFactory(printWork);
            using var client = factory.CreateClient();
            var ownerJwt = await SeedPilotOwnerWithLocationRoomAsync(
                factory.Services
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", ownerJwt);
            request.Content = JsonContent.Create(new
            {
                locationName = "Later Location",
                address = "3 Briggate",
                city = "Leeds",
                postcode = "LS1 6ER",
            });

            using var response = await client
                .SendAsync(request)
                .WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content
                .ReadFromJsonAsync<JsonElement>();
            var locationId = body.GetProperty("locationId").GetInt32();

            Assert.Equal(locationId, await printWork.NextRequestAsync());
            await printWork.WaitForCompletedAsync(1);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var qrCodes = await context.QrCodes
                .Where(row => row.RestaurantLocationId == locationId)
                .ToListAsync();

            Assert.Equal(3, qrCodes.Count);
            Assert.Contains(qrCodes, row => row.QrType == QrType.TableTent);
            Assert.Contains(qrCodes, row => row.QrType == QrType.WindowSticker);
            Assert.Contains(qrCodes, row => row.QrType == QrType.OfferCard);
        }

        private WebApplicationFactory<Program> CreateFactory(
            ControllablePrintReadyQrMaterialsWork printWork
        )
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IPrintReadyQrMaterialsWork>();
                    services.AddSingleton<IPrintReadyQrMaterialsWork>(printWork);
                });
            });
        }

        private static async Task SeedInviteAsync(
            IServiceProvider services,
            string email,
            string token,
            string businessName,
            string accountType
        )
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            context.TrialRequests.Add(new TrialRequest
            {
                BusinessName = businessName,
                BusinessCategory = "multi-site",
                Locations = accountType == "Multi" ? "2" : "1",
                FullName = "Starter Owner",
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
                AccountType = accountType,
                CreatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        private static async Task<string> SeedPilotOwnerWithLocationRoomAsync(
            IServiceProvider services
        )
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();
            var owner = new User
            {
                FullName = "Later Location Owner",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"Later Location Group {Guid.NewGuid():N}",
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
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            owner.SelectedRestaurantId = restaurant.Id;
            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                owner.Id.ToString(),
                owner.Email,
                owner.Role
            );
        }

        private sealed class ControllablePrintReadyQrMaterialsWork
            : IPrintReadyQrMaterialsWork
        {
            private readonly Func<int, Task> _process;
            private readonly Channel<int> _requests = Channel.CreateUnbounded<int>();
            private readonly Channel<int> _completed = Channel.CreateUnbounded<int>();
            private int _completedCount;

            public ControllablePrintReadyQrMaterialsWork(
                Func<int, Task> process
            )
            {
                _process = process;
            }

            public int CompletedCount => Volatile.Read(ref _completedCount);

            public ValueTask RequestEnsureAsync(
                int locationId,
                CancellationToken cancellationToken = default
            )
            {
                _requests.Writer.TryWrite(locationId);
                _ = ProcessGuardedAsync(locationId);
                return ValueTask.CompletedTask;
            }

            public ValueTask RequestShopOrderEnsureAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public Task RunAsync(CancellationToken stoppingToken)
                => Task.CompletedTask;

            public Task DrainAsync(
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;

            public async Task<int> NextRequestAsync()
            {
                return await _requests.Reader
                    .ReadAsync()
                    .AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(5));
            }

            public async Task WaitForCompletedAsync(int expected)
            {
                for (var completed = 0; completed < expected; completed++)
                {
                    await _completed.Reader
                        .ReadAsync()
                        .AsTask()
                        .WaitAsync(TimeSpan.FromSeconds(5));
                }
            }

            private async Task ProcessGuardedAsync(int locationId)
            {
                try
                {
                    await _process(locationId);
                }
                catch
                {
                    // The fake records completion after a controlled failure.
                }
                finally
                {
                    Interlocked.Increment(ref _completedCount);
                    _completed.Writer.TryWrite(locationId);
                }
            }
        }
    }
}
