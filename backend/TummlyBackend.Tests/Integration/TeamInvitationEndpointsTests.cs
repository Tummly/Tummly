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
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class TeamInvitationEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public TeamInvitationEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
        }

        [Fact]
        public async Task Owner_SendInvite_CreatesPendingRow_WithoutMembership()
        {
            var tracking = new TrackingTeamInvitationEmailService();
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedWorkspaceAsync();
            var email = $"{Guid.NewGuid():N}@example.com";

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/team-permissions/invitations",
                seeded.OwnerJwt,
                new
                {
                    email,
                    fullName = "Mark Invitee",
                    permissionRole = PermissionRoles.ReportingOnly,
                    locationScope = "all",
                    namedLocationIds = Array.Empty<int>(),
                    message = "Welcome aboard",
                }
            );
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var invite = await context.TeamInvitations
                .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(email, invite.Email);
            Assert.Equal("Mark Invitee", invite.FullName);
            Assert.Equal(PermissionRoles.ReportingOnly, invite.PermissionRole);
            Assert.Equal(LocationScopeKind.AllLocations, invite.LocationScope);
            Assert.Equal("Welcome aboard", invite.Message);
            Assert.False(string.IsNullOrWhiteSpace(invite.OpaqueReference));
            Assert.True(invite.ExpiresAt > DateTime.UtcNow.AddDays(6));
            Assert.True(invite.ExpiresAt <= DateTime.UtcNow.AddDays(7).AddMinutes(1));
            Assert.False(
                await context.RestaurantMemberships.AnyAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.User.Email == email
                )
            );
            Assert.True(
                await context.RestaurantAccessActivities.AnyAsync(row =>
                    row.Kind == AccessActivityKinds.InvitationSent
                    && row.TargetEmail == email
                    && row.ActorUserId == seeded.OwnerUserId
                )
            );
            Assert.Single(tracking.Sent);
            Assert.Equal(email, tracking.Sent[0].ToEmail);
            Assert.Contains("Team Venue", tracking.Sent[0].Subject);
            Assert.Contains("/start?invite=", tracking.Sent[0].AcceptUrl);
        }

        [Fact]
        public async Task SendInvite_Returns400_WhenEmailAlreadyPending()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var seeded = await SeedWorkspaceAsync();
            var email = $"{Guid.NewGuid():N}@example.com";
            await SendInviteAsync(client, seeded.OwnerJwt, email);
            var second = await SendInviteAsync(client, seeded.OwnerJwt, email);
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            var body = await second.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                "An invitation is already pending for this email.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task SendInvite_Returns400_WhenEmailIsActiveMember()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var seeded = await SeedWorkspaceAsync();
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var staffEmail = (await context.Users.FirstAsync(row => row.Id == seeded.StaffUserId)).Email;
            var response = await SendInviteAsync(client, seeded.OwnerJwt, staffEmail);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SendInvite_Returns403_WhenAdminInvitesAdmin()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/team-permissions/invitations",
                seeded.AdminJwt,
                InviteBody($"{Guid.NewGuid():N}@example.com", PermissionRoles.Admin)
            );
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SendInvite_Succeeds_WhenWorkspaceIsPaused()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var seeded = await SeedWorkspaceAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var restaurant = await context.Restaurants.FirstAsync(row => row.Id == seeded.RestaurantId);
                restaurant.WorkspaceStatus = WorkspaceStatus.Paused;
                await context.SaveChangesAsync();
            }

            var response = await SendInviteAsync(
                client,
                seeded.OwnerJwt,
                $"{Guid.NewGuid():N}@example.com"
            );
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Resend_RotatesReference_AndSendsAgain()
        {
            var tracking = new TrackingTeamInvitationEmailService();
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedWorkspaceAsync();
            var email = $"{Guid.NewGuid():N}@example.com";
            await SendInviteAsync(client, seeded.OwnerJwt, email);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var invite = await context.TeamInvitations.SingleAsync(row =>
                row.Email == email
            );
            var oldRef = invite.OpaqueReference;

            using var resend = AuthorizedJson(
                HttpMethod.Post,
                $"/api/team-permissions/invitations/{invite.Id}/resend",
                seeded.OwnerJwt,
                new { }
            );
            var response = await client.SendAsync(resend);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            await context.Entry(invite).ReloadAsync();
            Assert.NotEqual(oldRef, invite.OpaqueReference);
            Assert.Equal(2, tracking.Sent.Count);
            Assert.True(
                await context.RestaurantAccessActivities.AnyAsync(row =>
                    row.Kind == AccessActivityKinds.InvitationResent
                )
            );
        }

        [Fact]
        public async Task Revoke_DropsRow_AndDoesNotEmail()
        {
            var tracking = new TrackingTeamInvitationEmailService();
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedWorkspaceAsync();
            var email = $"{Guid.NewGuid():N}@example.com";
            await SendInviteAsync(client, seeded.OwnerJwt, email);
            tracking.Sent.Clear();

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var invite = await context.TeamInvitations.SingleAsync(row =>
                row.Email == email
            );

            using var revoke = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/team-permissions/invitations/{invite.Id}"
            );
            revoke.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            var response = await client.SendAsync(revoke);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            context.ChangeTracker.Clear();
            Assert.False(await context.TeamInvitations.AnyAsync(row => row.Email == email));
            Assert.Empty(tracking.Sent);
            Assert.True(
                await context.RestaurantAccessActivities.AnyAsync(row =>
                    row.Kind == AccessActivityKinds.InvitationRevoked
                )
            );
        }

        [Fact]
        public async Task GetPage_ListsPendingAndExpiredInvitations()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var seeded = await SeedWorkspaceAsync();
            var liveEmail = $"{Guid.NewGuid():N}@example.com";
            await SendInviteAsync(client, seeded.OwnerJwt, liveEmail);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.TeamInvitations.Add(
                    new TeamInvitation
                    {
                        RestaurantId = seeded.RestaurantId,
                        Email = $"{Guid.NewGuid():N}@expired.example",
                        FullName = "Expired Person",
                        PermissionRole = PermissionRoles.Staff,
                        LocationScope = LocationScopeKind.AllLocations,
                        NamedLocationIdsJson = "[]",
                        InviterUserId = seeded.OwnerUserId,
                        SentAt = DateTime.UtcNow.AddDays(-8),
                        ExpiresAt = DateTime.UtcNow.AddDays(-1),
                        OpaqueReference = TeamInvitationReference.Create(),
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/team-permissions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("stats").GetProperty("pendingInvites").GetInt32() >= 1);
            var invitations = body.GetProperty("invitations");
            Assert.Contains(
                invitations.EnumerateArray(),
                row => row.GetProperty("email").GetString() == liveEmail
                    && !row.GetProperty("expired").GetBoolean()
            );
            Assert.Contains(
                invitations.EnumerateArray(),
                row => row.GetProperty("expired").GetBoolean()
            );
        }

        [Fact]
        public async Task Preview_Returns400_ForUnknownOrExpiredReference()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var missing = await client.GetAsync("/api/team-invitations/preview?invite=not-a-real-ref");
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

            var empty = await client.GetAsync("/api/team-invitations/preview?invite=");
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        }

        [Fact]
        public async Task Accept_NewUser_CredentialsThenOtp_CreatesMembership()
        {
            var tracking = new TrackingTeamInvitationEmailService();
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedWorkspaceAsync();
            var email = $"{Guid.NewGuid():N}@new.example";
            await SendInviteAsync(client, seeded.OwnerJwt, email);

            string opaque;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                opaque = (await context.TeamInvitations.SingleAsync(row =>
                    row.Email == email
                )).OpaqueReference;
            }

            var credentials = await client.PostAsJsonAsync(
                "/api/team-invitations/credentials",
                new { invite = opaque, fullName = "New Member", password = "Password1" }
            );
            Assert.Equal(HttpStatusCode.OK, credentials.StatusCode);

            string otp;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                otp = (await context.OtpVerifications.SingleAsync(row => row.Email == email)).OtpCode;
            }

            var verify = await client.PostAsJsonAsync(
                "/api/team-invitations/verify-otp",
                new { invite = opaque, email, otpCode = otp }
            );
            Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
            var body = await verify.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("activationRequired").GetBoolean());

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.False(await context.TeamInvitations.AnyAsync(row => row.Email == email));
                var user = await context.Users.SingleAsync(row => row.Email == email);
                Assert.NotNull(user.ActivatedAt);
                Assert.True(user.HasCompletedFirstSignIn);
                Assert.True(
                    await context.RestaurantMemberships.AnyAsync(row =>
                        row.UserId == user.Id
                        && row.RestaurantId == seeded.RestaurantId
                        && row.PermissionRole == PermissionRoles.ReportingOnly
                        && row.Status == MembershipStatus.Active
                    )
                );
                Assert.True(
                    await context.RestaurantAccessActivities.AnyAsync(row =>
                        row.Kind == AccessActivityKinds.InvitationAccepted
                        && row.ActorUserId == user.Id
                    )
                );
            }
        }

        [Fact]
        public async Task Accept_ExistingUser_SignInThenMembership()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var seeded = await SeedWorkspaceAsync();
            const string password = "Password1";
            string email;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var other = AddUser(context, "Other Venue Owner", "Owner");
                other.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await context.SaveChangesAsync();
                email = other.Email;
                var otherRestaurant = new Restaurant
                {
                    Name = "Other Venue",
                    AccountType = "Single",
                    OwnerUserId = other.Id,
                    BillingContactUserId = other.Id,
                    PrivacyContactUserId = other.Id,
                    SupportContactUserId = other.Id,
                    CreatedAt = DateTime.UtcNow,
                };
                context.Restaurants.Add(otherRestaurant);
                await context.SaveChangesAsync();
                AddMembership(
                    context,
                    other.Id,
                    otherRestaurant.Id,
                    PermissionRoles.Owner,
                    LocationScopeKind.AllLocations,
                    "[]"
                );
                await context.SaveChangesAsync();
            }

            await SendInviteAsync(client, seeded.OwnerJwt, email);
            string opaque;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                opaque = (await context.TeamInvitations.SingleAsync(row =>
                    row.Email == email
                )).OpaqueReference;
            }

            var signIn = await client.PostAsJsonAsync(
                "/api/team-invitations/sign-in",
                new { invite = opaque, password }
            );
            Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);

            string otp;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                otp = (await context.OtpVerifications
                    .OrderByDescending(row => row.Id)
                    .FirstAsync(row => row.Email == email)).OtpCode;
            }

            var verify = await client.PostAsJsonAsync(
                "/api/team-invitations/verify-otp",
                new { invite = opaque, email, otpCode = otp }
            );
            Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
            var body = await verify.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("workspaceCount").GetInt32() >= 2);
        }

        [Fact]
        public async Task Accept_LoggedInWrongEmail_RequiresSignOut()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var seeded = await SeedWorkspaceAsync();
            var email = $"{Guid.NewGuid():N}@new.example";
            await SendInviteAsync(client, seeded.OwnerJwt, email);
            string opaque;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                opaque = (await context.TeamInvitations.SingleAsync(row =>
                    row.Email == email
                )).OpaqueReference;
            }

            using var preview = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/team-invitations/preview?invite={Uri.EscapeDataString(opaque)}"
            );
            preview.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            var response = await client.SendAsync(preview);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("wrong-email", body.GetProperty("session").GetString());
            Assert.Equal(email, body.GetProperty("email").GetString());
        }

        [Fact]
        public async Task Accept_PausedWorkspace_StillCreatesMembership()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var seeded = await SeedWorkspaceAsync();
            var email = $"{Guid.NewGuid():N}@pause.example";
            await SendInviteAsync(client, seeded.OwnerJwt, email);
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var restaurant = await context.Restaurants.FirstAsync(row => row.Id == seeded.RestaurantId);
                restaurant.WorkspaceStatus = WorkspaceStatus.Paused;
                await context.SaveChangesAsync();
            }

            string opaque;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                opaque = (await context.TeamInvitations.SingleAsync(row =>
                    row.Email == email
                )).OpaqueReference;
            }

            var credentials = await client.PostAsJsonAsync(
                "/api/team-invitations/credentials",
                new { invite = opaque, fullName = "Paused Join", password = "Password1" }
            );
            Assert.Equal(HttpStatusCode.OK, credentials.StatusCode);
            string otp;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                otp = (await context.OtpVerifications.SingleAsync(row => row.Email == email)).OtpCode;
            }

            var verify = await client.PostAsJsonAsync(
                "/api/team-invitations/verify-otp",
                new { invite = opaque, email, otpCode = otp }
            );
            Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        }

        [Fact]
        public async Task Accept_FollowsOwnerPendingActivation_OnRestaurantApis()
        {
            var client = CreateClientWithEmail(new TrackingTeamInvitationEmailService());
            var seeded = await SeedWorkspaceAsync();
            var email = $"{Guid.NewGuid():N}@wait.example";
            await SendInviteAsync(client, seeded.OwnerJwt, email);
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var owner = await context.Users.FirstAsync(row => row.Id == seeded.OwnerUserId);
                owner.ActivatedAt = null;
                owner.ActivationCodeHash = "pending";
                await context.SaveChangesAsync();
            }
            string opaque;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                opaque = (await context.TeamInvitations.SingleAsync(row =>
                    row.Email == email
                )).OpaqueReference;
            }

            await client.PostAsJsonAsync(
                "/api/team-invitations/credentials",
                new { invite = opaque, fullName = "Waiter", password = "Password1" }
            );
            string otp;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                otp = (await context.OtpVerifications.SingleAsync(row => row.Email == email)).OtpCode;
            }

            var verify = await client.PostAsJsonAsync(
                "/api/team-invitations/verify-otp",
                new { invite = opaque, email, otpCode = otp }
            );
            var body = await verify.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("pending", body.GetProperty("ownerActivation").GetString());
            Assert.False(body.GetProperty("activationRequired").GetBoolean());

            var jwt = body.GetProperty("token").GetString();
            using var locations = new HttpRequestMessage(HttpMethod.Get, "/api/restaurant/locations");
            locations.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var locResponse = await client.SendAsync(locations);
            Assert.Equal(HttpStatusCode.Forbidden, locResponse.StatusCode);
            var locBody = await locResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(
                locBody.TryGetProperty("activationRequired", out var flag) && flag.GetBoolean()
            );
        }

        private static object InviteBody(string email, string role = PermissionRoles.ReportingOnly)
        {
            return new
            {
                email,
                fullName = "Mark Invitee",
                permissionRole = role,
                locationScope = "all",
                namedLocationIds = Array.Empty<int>(),
                message = "Welcome aboard",
            };
        }

        private static async Task<HttpResponseMessage> SendInviteAsync(
            HttpClient client,
            string jwt,
            string email
        )
        {
            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/team-permissions/invitations",
                jwt,
                InviteBody(email)
            );
            return await client.SendAsync(request);
        }

        private HttpClient CreateClientWithEmail(
            TrackingTeamInvitationEmailService email
        )
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(IEmailService))
                        .ToList();
                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<IEmailService>(email);
                });
            }).CreateClient();
        }

        private async Task<Seeded> SeedWorkspaceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = AddUser(context, "Owner Seventeen", "Owner");
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Team Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var locA = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var locB = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(locA, locB);
            await context.SaveChangesAsync();

            var ownerMembership = AddMembership(
                context,
                owner.Id,
                restaurant.Id,
                PermissionRoles.Owner,
                LocationScopeKind.AllLocations,
                "[]"
            );

            var admin = AddUser(context, "Admin Seventeen", "Owner");
            admin.SelectedRestaurantId = restaurant.Id;
            var staff = AddUser(context, "Staff Seventeen", "Owner");
            staff.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            var adminMembership = AddMembership(
                context,
                admin.Id,
                restaurant.Id,
                PermissionRoles.Admin,
                LocationScopeKind.AllLocations,
                "[]"
            );
            var staffMembership = AddMembership(
                context,
                staff.Id,
                restaurant.Id,
                PermissionRoles.Staff,
                LocationScopeKind.NamedList,
                MembershipLocationScope.SerializeNamedIds([locA.Id])
            );
            await context.SaveChangesAsync();

            return new Seeded(
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                jwtService.GenerateToken(admin.Id.ToString(), admin.Email, admin.Role),
                jwtService.GenerateToken(staff.Id.ToString(), staff.Email, staff.Role),
                restaurant.Id,
                owner.Id,
                admin.Id,
                staff.Id,
                ownerMembership.Id,
                adminMembership.Id,
                staffMembership.Id,
                locA.Id,
                locB.Id
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

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body
        )
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(body);
            return request;
        }

        private sealed record Seeded(
            string OwnerJwt,
            string AdminJwt,
            string StaffJwt,
            int RestaurantId,
            int OwnerUserId,
            int AdminUserId,
            int StaffUserId,
            int OwnerMembershipId,
            int AdminMembershipId,
            int StaffMembershipId,
            int LocationAId,
            int LocationBId
        );

        private sealed class TrackingTeamInvitationEmailService : EmailServiceStubBase
        {
            public List<(
                string ToEmail,
                string Subject,
                string AcceptUrl
            )> Sent { get; } = [];

            public override Task SendTeamInvitationEmailAsync(
                string toEmail,
                string subject,
                string acceptUrl,
                string firstName,
                string inviterName,
                string workspaceName,
                string roleName,
                string locationScope,
                string? invitationMessage
            )
            {
                Sent.Add((toEmail, subject, acceptUrl));
                return Task.CompletedTask;
            }
        }
    }
}
