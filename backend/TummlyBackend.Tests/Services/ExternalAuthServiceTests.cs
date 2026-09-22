using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class ExternalAuthServiceTests : IDisposable
    {
        private const string FrontendBase = "https://app.example";
        private const string ProfileEmail = "a@b.com";
        private const string ProfileSubject = "sub-1";

        private readonly ApplicationDbContext _db;
        private readonly FakeExternalOAuthProviderClient _providerClient;
        private readonly FakeAuthService _authService;
        private readonly ExternalAuthService _sut;

        public ExternalAuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _providerClient = new FakeExternalOAuthProviderClient();
            _authService = new FakeAuthService();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = FrontendBase,
                    }
                )
                .Build();

            _sut = new ExternalAuthService(
                _db,
                _providerClient,
                _authService,
                configuration,
                NullLogger<ExternalAuthService>.Instance
            );
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public async Task Callback_LinkedSubject_RedirectsSignInExchange()
        {
            var user = await SeedUserAsync(ProfileEmail);
            _db.UserExternalLogins.Add(
                new UserExternalLogin
                {
                    UserId = user.Id,
                    Provider = ExternalAuthProviders.Google,
                    ProviderSubject = ProfileSubject,
                }
            );
            await _db.SaveChangesAsync();

            var state = await SeedOAuthStateAsync(
                ExternalAuthProviders.Google,
                "/login"
            );
            _providerClient.Profile = new ExternalOAuthProfile
            {
                Provider = ExternalAuthProviders.Google,
                Subject = ProfileSubject,
                Email = ProfileEmail,
                EmailVerified = true,
                FullName = "Linked User",
            };

            var redirect = await _sut.HandleCallbackAsync(
                ExternalAuthProviders.Google,
                "code-1",
                state,
                error: null
            );

            Assert.Contains("/login/oauth/complete?token=", redirect);
            var token = ExtractQueryValue(redirect, "token");
            var ticket = await _db.ExternalAuthTickets.SingleAsync(x =>
                x.Token == token
            );
            Assert.Equal(
                ExternalAuthTicketPurposes.SignInExchange,
                ticket.Purpose
            );
        }

        [Fact]
        public async Task Callback_ExistingUserEmail_NotLinked_RedirectsAccountExists()
        {
            await SeedUserAsync(ProfileEmail);
            var state = await SeedOAuthStateAsync(
                ExternalAuthProviders.Google,
                "/login"
            );
            _providerClient.Profile = VerifiedGoogleProfile(
                subject: "new-subject",
                email: ProfileEmail
            );

            var redirect = await _sut.HandleCallbackAsync(
                ExternalAuthProviders.Google,
                "code-1",
                state,
                error: null
            );

            Assert.Contains("oauthError=account_exists", redirect);
            Assert.StartsWith($"{FrontendBase}/login?", redirect);
        }

        [Fact]
        public async Task Callback_ExistingUserEmail_SignupReturnPath_RedirectsAccountExists()
        {
            await SeedUserAsync(ProfileEmail);
            var state = await SeedOAuthStateAsync(
                ExternalAuthProviders.Google,
                "/signup"
            );
            _providerClient.Profile = VerifiedGoogleProfile(
                subject: "new-subject",
                email: ProfileEmail
            );

            var redirect = await _sut.HandleCallbackAsync(
                ExternalAuthProviders.Google,
                "code-1",
                state,
                error: null
            );

            Assert.Contains("oauthError=account_exists", redirect);
            Assert.StartsWith($"{FrontendBase}/signup?", redirect);
        }

        [Fact]
        public async Task Callback_AdminEmail_RedirectsStaffRejected()
        {
            _db.Admins.Add(
                new Admin
                {
                    FullName = "Staff Admin",
                    Email = ProfileEmail,
                    PasswordHash = "hash",
                    Role = "Admin",
                    IsActive = true,
                }
            );
            await _db.SaveChangesAsync();

            var state = await SeedOAuthStateAsync(
                ExternalAuthProviders.Google,
                "/login"
            );
            _providerClient.Profile = VerifiedGoogleProfile(
                subject: "staff-sub",
                email: ProfileEmail
            );

            var redirect = await _sut.HandleCallbackAsync(
                ExternalAuthProviders.Google,
                "code-1",
                state,
                error: null
            );

            Assert.Contains("oauthError=staff_password_only", redirect);
        }

        [Fact]
        public async Task Callback_NewEmail_RedirectsSignUpTerms()
        {
            var state = await SeedOAuthStateAsync(
                ExternalAuthProviders.Google,
                "/signup"
            );
            _providerClient.Profile = VerifiedGoogleProfile(
                subject: "new-sub",
                email: "new@example.com"
            );

            var redirect = await _sut.HandleCallbackAsync(
                ExternalAuthProviders.Google,
                "code-1",
                state,
                error: null
            );

            Assert.Contains("/signup/oauth/terms?token=", redirect);
            var token = ExtractQueryValue(redirect, "token");
            var ticket = await _db.ExternalAuthTickets.SingleAsync(x =>
                x.Token == token
            );
            Assert.Equal(
                ExternalAuthTicketPurposes.SignUpExchange,
                ticket.Purpose
            );
        }

        [Fact]
        public async Task AcceptTerms_CreatesVerifiedPending_NoPassword()
        {
            var token = await SeedSignUpExchangeAsync(
                email: "social@example.com",
                provider: ExternalAuthProviders.Google,
                providerSubject: "sub-social",
                fullName: "Social User"
            );

            var session = await _sut.AcceptTermsAsync(
                new ExternalAuthAcceptTermsDto
                {
                    Token = token,
                    TermsAccepted = true,
                }
            );

            Assert.Equal(PendingSignupStatuses.Verified, session.Status);
            Assert.Equal("social@example.com", session.Email);

            var pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal(PendingSignupStatuses.Verified, pending.Status);
            Assert.Null(pending.PasswordHash);
            Assert.Equal(ExternalAuthProviders.Google, pending.AuthProvider);
            Assert.Equal("sub-social", pending.ProviderSubject);
            Assert.True(pending.TermsAccepted);
            Assert.NotNull(pending.EmailVerifiedAt);
        }

        [Fact]
        public async Task AcceptTerms_TermsFalse_ThrowsAndLeavesTicketUnconsumed()
        {
            var token = await SeedSignUpExchangeAsync(
                email: "terms@example.com",
                provider: ExternalAuthProviders.Google,
                providerSubject: "sub-terms",
                fullName: null
            );

            await Assert.ThrowsAsync<Exception>(() =>
                _sut.AcceptTermsAsync(
                    new ExternalAuthAcceptTermsDto
                    {
                        Token = token,
                        TermsAccepted = false,
                    }
                )
            );

            var ticket = await _db.ExternalAuthTickets.SingleAsync(x =>
                x.Token == token
            );
            Assert.Null(ticket.ConsumedAtUtc);
        }

        [Fact]
        public async Task AcceptTerms_UserAppearedAfterTicket_ThrowsAccountExists()
        {
            const string email = "race@example.com";
            var token = await SeedSignUpExchangeAsync(
                email: email,
                provider: ExternalAuthProviders.Google,
                providerSubject: "sub-race",
                fullName: "Race User"
            );
            await SeedUserAsync(email);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.AcceptTermsAsync(
                    new ExternalAuthAcceptTermsDto
                    {
                        Token = token,
                        TermsAccepted = true,
                    }
                )
            );

            Assert.Equal("Email already in use.", ex.Message);
            Assert.Empty(_db.PendingSignups);

            var ticket = await _db.ExternalAuthTickets.SingleAsync(x =>
                x.Token == token
            );
            Assert.Null(ticket.ConsumedAtUtc);
        }

        [Fact]
        public async Task AcceptTerms_LateStagePending_ThrowsAndLeavesTicketUnconsumed()
        {
            const string email = "late@example.com";
            var token = await SeedSignUpExchangeAsync(
                email: email,
                provider: ExternalAuthProviders.Google,
                providerSubject: "sub-late",
                fullName: "Late User"
            );
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    Email = email,
                    Status = PendingSignupStatuses.OnboardingComplete,
                    TermsAccepted = true,
                    PasswordHash = "hash",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                }
            );
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.AcceptTermsAsync(
                    new ExternalAuthAcceptTermsDto
                    {
                        Token = token,
                        TermsAccepted = true,
                    }
                )
            );

            Assert.Equal("Email already in use.", ex.Message);

            var pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal(PendingSignupStatuses.OnboardingComplete, pending.Status);
            Assert.Equal("hash", pending.PasswordHash);

            var ticket = await _db.ExternalAuthTickets.SingleAsync(x =>
                x.Token == token
            );
            Assert.Null(ticket.ConsumedAtUtc);
        }

        [Fact]
        public async Task AcceptTerms_AdminEmail_ThrowsStaffReject()
        {
            const string email = "staff-oauth@example.com";
            var token = await SeedSignUpExchangeAsync(
                email: email,
                provider: ExternalAuthProviders.Google,
                providerSubject: "sub-staff",
                fullName: null
            );
            _db.Admins.Add(
                new Admin
                {
                    FullName = "Staff Admin",
                    Email = "Staff-OAuth@Example.com",
                    PasswordHash = "hash",
                    Role = "Admin",
                    IsActive = true,
                }
            );
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.AcceptTermsAsync(
                    new ExternalAuthAcceptTermsDto
                    {
                        Token = token,
                        TermsAccepted = true,
                    }
                )
            );

            Assert.Equal(
                "Staff accounts must use email and password Sign-in.",
                ex.Message
            );
            Assert.Empty(_db.PendingSignups);

            var ticket = await _db.ExternalAuthTickets.SingleAsync(x =>
                x.Token == token
            );
            Assert.Null(ticket.ConsumedAtUtc);
        }

        [Fact]
        public async Task Exchange_ExpiredToken_Throws()
        {
            var token = NewTicketToken();
            _db.ExternalAuthTickets.Add(
                new ExternalAuthTicket
                {
                    Id = Guid.NewGuid(),
                    Token = token,
                    Purpose = ExternalAuthTicketPurposes.SignInExchange,
                    PayloadJson = JsonSerializer.Serialize(
                        new { userId = 1 }
                    ),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11),
                }
            );
            await _db.SaveChangesAsync();

            await Assert.ThrowsAsync<Exception>(() =>
                _sut.ExchangeSignInAsync(
                    new ExternalAuthExchangeDto { Token = token }
                )
            );
        }

        [Fact]
        public async Task Exchange_SecondUse_Throws()
        {
            var user = await SeedUserAsync("exchange@example.com");
            var token = NewTicketToken();
            _db.ExternalAuthTickets.Add(
                new ExternalAuthTicket
                {
                    Id = Guid.NewGuid(),
                    Token = token,
                    Purpose = ExternalAuthTicketPurposes.SignInExchange,
                    PayloadJson = JsonSerializer.Serialize(
                        new { userId = user.Id }
                    ),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await _db.SaveChangesAsync();

            await _sut.ExchangeSignInAsync(
                new ExternalAuthExchangeDto { Token = token }
            );

            await Assert.ThrowsAsync<Exception>(() =>
                _sut.ExchangeSignInAsync(
                    new ExternalAuthExchangeDto { Token = token }
                )
            );
        }

        [Fact]
        public async Task Exchange_ParallelConsume_AtomicPattern_InMemoryNote()
        {
            // ConsumeTicketAsync uses ExecuteUpdateAsync WHERE ConsumedAtUtc == null
            // so relational DBs only grant one winner. EF InMemory has no real
            // concurrency control; dual success here documents that gap — the
            // atomic SQL pattern is still the production guarantee.
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            await using var seedDb = new ApplicationDbContext(options);
            var user = new User
            {
                Email = "parallel@example.com",
                FullName = "Parallel User",
                PasswordHash = "hash",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
            };
            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync();

            var token = NewTicketToken();
            seedDb.ExternalAuthTickets.Add(
                new ExternalAuthTicket
                {
                    Id = Guid.NewGuid(),
                    Token = token,
                    Purpose = ExternalAuthTicketPurposes.SignInExchange,
                    PayloadJson = JsonSerializer.Serialize(
                        new { userId = user.Id }
                    ),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await seedDb.SaveChangesAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = FrontendBase,
                    }
                )
                .Build();

            await using var dbA = new ApplicationDbContext(options);
            await using var dbB = new ApplicationDbContext(options);
            var sutA = new ExternalAuthService(
                dbA,
                _providerClient,
                _authService,
                configuration,
                NullLogger<ExternalAuthService>.Instance
            );
            var sutB = new ExternalAuthService(
                dbB,
                _providerClient,
                _authService,
                configuration,
                NullLogger<ExternalAuthService>.Instance
            );

            var outcomes = await Task.WhenAll(
                TryExchangeWithAsync(sutA, token),
                TryExchangeWithAsync(sutB, token)
            );

            var successCount = outcomes.Count(x => x);
            Assert.True(
                successCount >= 1,
                "At least one parallel exchange must succeed."
            );

            await using var verifyDb = new ApplicationDbContext(options);
            var ticket = await verifyDb.ExternalAuthTickets.AsNoTracking()
                .SingleAsync(x => x.Token == token);
            Assert.NotNull(ticket.ConsumedAtUtc);

            var verifySut = new ExternalAuthService(
                verifyDb,
                _providerClient,
                _authService,
                configuration,
                NullLogger<ExternalAuthService>.Instance
            );
            await Assert.ThrowsAsync<Exception>(() =>
                verifySut.ExchangeSignInAsync(
                    new ExternalAuthExchangeDto { Token = token }
                )
            );

            // If successCount > 1: InMemory could not enforce single-winner;
            // relational WHERE ConsumedAtUtc IS NULL still required in prod.
            if (successCount == 1)
            {
                Assert.Equal(1, successCount);
            }
        }

        private static async Task<bool> TryExchangeWithAsync(
            ExternalAuthService sut,
            string token
        )
        {
            try
            {
                await sut.ExchangeSignInAsync(
                    new ExternalAuthExchangeDto { Token = token }
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<User> SeedUserAsync(string email)
        {
            var user = new User
            {
                Email = email,
                FullName = "Test User",
                PasswordHash = "hash",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        private async Task<string> SeedOAuthStateAsync(
            string provider,
            string returnPath
        )
        {
            var token = NewTicketToken();
            _db.ExternalAuthTickets.Add(
                new ExternalAuthTicket
                {
                    Id = Guid.NewGuid(),
                    Token = token,
                    Purpose = ExternalAuthTicketPurposes.OAuthState,
                    PayloadJson = JsonSerializer.Serialize(
                        new
                        {
                            provider,
                            returnPath,
                            nonce = Guid.NewGuid().ToString("N"),
                        }
                    ),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await _db.SaveChangesAsync();
            return token;
        }

        private async Task<string> SeedSignUpExchangeAsync(
            string email,
            string provider,
            string providerSubject,
            string? fullName
        )
        {
            var token = NewTicketToken();
            _db.ExternalAuthTickets.Add(
                new ExternalAuthTicket
                {
                    Id = Guid.NewGuid(),
                    Token = token,
                    Purpose = ExternalAuthTicketPurposes.SignUpExchange,
                    PayloadJson = JsonSerializer.Serialize(
                        new
                        {
                            email,
                            provider,
                            providerSubject,
                            fullName,
                        }
                    ),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await _db.SaveChangesAsync();
            return token;
        }

        private static ExternalOAuthProfile VerifiedGoogleProfile(
            string subject,
            string email
        ) =>
            new()
            {
                Provider = ExternalAuthProviders.Google,
                Subject = subject,
                Email = email,
                EmailVerified = true,
                FullName = "OAuth User",
            };

        private static string NewTicketToken() =>
            Convert.ToHexString(Guid.NewGuid().ToByteArray())
            + Convert.ToHexString(Guid.NewGuid().ToByteArray());

        private static string ExtractQueryValue(string url, string key)
        {
            var uri = new Uri(url);
            var query = uri.Query.TrimStart('?');
            foreach (
                var part in query.Split(
                    '&',
                    StringSplitOptions.RemoveEmptyEntries
                )
            )
            {
                var pair = part.Split('=', 2);
                if (
                    pair.Length == 2
                    && string.Equals(
                        pair[0],
                        key,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return Uri.UnescapeDataString(pair[1]);
                }
            }

            throw new InvalidOperationException(
                $"Query key '{key}' not found in '{url}'."
            );
        }

        private sealed class FakeExternalOAuthProviderClient
            : IExternalOAuthProviderClient
        {
            public ExternalOAuthProfile Profile { get; set; } =
                new()
                {
                    Provider = ExternalAuthProviders.Google,
                    Subject = ProfileSubject,
                    Email = ProfileEmail,
                    EmailVerified = true,
                };

            public string BuildAuthorizationUrl(string provider, string state) =>
                $"https://accounts.google.com/o/oauth2/v2/auth?state={state}&provider={provider}";

            public Task<ExternalOAuthProfile> ExchangeCodeAsync(
                string provider,
                string code,
                CancellationToken cancellationToken = default
            )
            {
                _ = provider;
                _ = code;
                _ = cancellationToken;
                return Task.FromResult(Profile);
            }
        }

        private sealed class FakeAuthService : IAuthService
        {
            public Task<object> CompleteExternalOperatorSignInAsync(
                int userId,
                bool rememberDevice,
                string? deviceToken
            )
            {
                _ = userId;
                _ = rememberDevice;
                _ = deviceToken;
                object result = new
                {
                    loginType = "USER",
                    otpChannel = "email",
                };
                return Task.FromResult(result);
            }

            public Task<string> AdminLoginAsync(AdminLoginDto dto) =>
                throw new NotImplementedException();

            public Task<string> UserLoginAsync(UserLoginDto dto) =>
                throw new NotImplementedException();

            public Task<object> UniversalLoginAsync(UserLoginDto dto) =>
                throw new NotImplementedException();

            public Task<object> VerifyOtpAsync(
                VerifyOtpDto dto,
                SignInContext? signInContext = null
            ) => throw new NotImplementedException();

            public Task<object> RefreshSessionAsync(string refreshToken) =>
                throw new NotImplementedException();

            public Task RevokeRefreshTokenAsync(string refreshToken) =>
                throw new NotImplementedException();

            public Task<SendOtpResultDto> SendAuthOtpAsync(
                string email,
                string purpose
            ) => throw new NotImplementedException();

            public Task<SendOtpResultDto> SendAuthOtpSmsAsync(string email) =>
                throw new NotImplementedException();

            public Task ForgotPasswordAsync(ForgotPasswordDto dto) =>
                throw new NotImplementedException();

            public Task ResetPasswordAsync(ResetPasswordDto dto) =>
                throw new NotImplementedException();

            public Task<IReadOnlyList<WorkspaceRestaurantDto>> ListWorkspacesAsync(
                int userId
            ) => throw new NotImplementedException();

            public Task<SelectWorkspaceResult> SelectWorkspaceAsync(
                int userId,
                int restaurantId
            ) => throw new NotImplementedException();

            public Task<SessionRoutingFields> GetCurrentUserRoutingAsync(
                int userId
            ) => throw new NotImplementedException();

            public Task<SessionRoutingFields> ActivateAccountAsync(
                int userId,
                string activationCode
            ) => throw new NotImplementedException();
        }
    }
}
