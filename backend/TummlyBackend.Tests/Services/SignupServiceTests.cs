using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class SignupServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly TrackingEmailService _emailService;
        private readonly SignupService _sut;

        public SignupServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _emailService = new TrackingEmailService();
            _sut = new SignupService(
                _db,
                _emailService,
                new StubProvisioningService()
            );
        }

        [Fact]
        public async Task StartAsync_CreatesPending_AndSendsOtp()
        {
            var result = await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "owner@example.com",
                    TermsAccepted = true,
                }
            );

            Assert.Equal(PendingSignupStatuses.EmailPending, result.Status);
            Assert.NotEqual(Guid.Empty, result.SessionToken);
            Assert.Equal("owner@example.com", result.Email);
            Assert.True(
                await _db.PendingSignups.AnyAsync(x =>
                    x.Email == "owner@example.com"
                )
            );
            Assert.Single(_emailService.SentOtpEmails);
            Assert.Equal("owner@example.com", _emailService.SentOtpEmails[0].Email);
            Assert.True(
                await _db.OtpVerifications.AnyAsync(x =>
                    x.Email == "owner@example.com" && !x.IsUsed
                )
            );
        }

        [Fact]
        public async Task StartAsync_EmailInUse_Throws()
        {
            _db.Users.Add(
                new User
                {
                    Email = "taken@example.com",
                    FullName = "Taken User",
                    PasswordHash = "hash",
                }
            );
            await _db.SaveChangesAsync();

            await Assert.ThrowsAsync<Exception>(() =>
                _sut.StartAsync(
                    new StartSignupDto
                    {
                        Email = "taken@example.com",
                        TermsAccepted = true,
                    }
                )
            );
        }

        [Fact]
        public async Task StartAsync_ResumesExistingEmailPending_NoDuplicate()
        {
            var first = await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "resume@example.com",
                    TermsAccepted = true,
                }
            );

            var pending = await _db.PendingSignups.SingleAsync();
            pending.LastOtpSentAt = DateTime.UtcNow.AddMinutes(-2);
            await _db.SaveChangesAsync();

            var second = await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "Resume@example.com",
                    TermsAccepted = true,
                }
            );

            Assert.Equal(first.SessionToken, second.SessionToken);
            Assert.Equal(1, await _db.PendingSignups.CountAsync());
            Assert.Equal(2, _emailService.SentOtpEmails.Count);
            pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal(1, pending.OtpResendCount);
        }

        [Fact]
        public async Task StartAsync_EmailPendingWithinCooldown_Throws()
        {
            await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "start-cooldown@example.com",
                    TermsAccepted = true,
                }
            );

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.StartAsync(
                    new StartSignupDto
                    {
                        Email = "start-cooldown@example.com",
                        TermsAccepted = true,
                    }
                )
            );

            Assert.Equal("Please wait before resending OTP.", ex.Message);
            Assert.Equal(1, _emailService.SentOtpEmails.Count);
            var pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal(0, pending.OtpResendCount);
        }

        [Fact]
        public async Task StartAsync_EmailPendingAtResendLimit_SetsAbandoned()
        {
            await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "start-limit@example.com",
                    TermsAccepted = true,
                }
            );

            var pending = await _db.PendingSignups.SingleAsync();
            pending.LastOtpSentAt = DateTime.UtcNow.AddMinutes(-2);
            pending.OtpResendCount = 5;
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.StartAsync(
                    new StartSignupDto
                    {
                        Email = "start-limit@example.com",
                        TermsAccepted = true,
                    }
                )
            );

            Assert.Equal("OTP resend limit reached.", ex.Message);
            pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal(PendingSignupStatuses.Abandoned, pending.Status);
            Assert.Equal(1, _emailService.SentOtpEmails.Count);
        }

        [Fact]
        public async Task StartAsync_ResumesVerified_SameSession_NoNewRow_NoOtp()
        {
            var sessionToken = Guid.NewGuid();
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    Email = "verified@example.com",
                    Status = PendingSignupStatuses.Verified,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                }
            );
            await _db.SaveChangesAsync();

            var result = await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "verified@example.com",
                    TermsAccepted = true,
                }
            );

            Assert.Equal(sessionToken, result.SessionToken);
            Assert.Equal(PendingSignupStatuses.Verified, result.Status);
            Assert.Equal(1, await _db.PendingSignups.CountAsync());
            Assert.Empty(_emailService.SentOtpEmails);
        }

        [Fact]
        public async Task StartAsync_CompletePending_ThrowsEmailInUse()
        {
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    Email = "complete@example.com",
                    Status = PendingSignupStatuses.Complete,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
                }
            );
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.StartAsync(
                    new StartSignupDto
                    {
                        Email = "complete@example.com",
                        TermsAccepted = true,
                    }
                )
            );
            Assert.Equal("Email already in use.", ex.Message);
            Assert.Equal(1, await _db.PendingSignups.CountAsync());
        }

        [Fact]
        public void PendingSignup_EmailHasUniqueIndex()
        {
            var entity = _db.Model.FindEntityType(typeof(PendingSignup));
            Assert.NotNull(entity);

            var emailIndex = entity!
                .GetIndexes()
                .Single(index =>
                    index.Properties.Select(p => p.Name).SequenceEqual(
                        new[] { "Email" }
                    )
                );
            Assert.True(emailIndex.IsUnique);
        }

        [Fact]
        public async Task StartAsync_ResetsAbandoned()
        {
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    Email = "abandoned@example.com",
                    Status = PendingSignupStatuses.Abandoned,
                    OtpResendCount = 5,
                    TermsAccepted = true,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
                }
            );
            await _db.SaveChangesAsync();

            var result = await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "abandoned@example.com",
                    TermsAccepted = true,
                }
            );

            Assert.Equal(PendingSignupStatuses.EmailPending, result.Status);
            var row = await _db.PendingSignups.SingleAsync();
            Assert.Equal(PendingSignupStatuses.EmailPending, row.Status);
            Assert.Equal(0, row.OtpResendCount);
        }

        [Fact]
        public async Task VerifyOtpAsync_ValidCode_SetsVerified()
        {
            var started = await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "verify@example.com",
                    TermsAccepted = true,
                }
            );

            var otp = await _db.OtpVerifications.SingleAsync(x =>
                x.Email == "verify@example.com" && !x.IsUsed
            );

            var session = await _sut.VerifyOtpAsync(
                new VerifySignupOtpDto
                {
                    Email = "verify@example.com",
                    OtpCode = otp.OtpCode,
                }
            );

            Assert.Equal(PendingSignupStatuses.Verified, session.Status);
            Assert.Equal(started.SessionToken, session.SessionToken);
            Assert.Equal("verify@example.com", session.Email);

            var pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal(PendingSignupStatuses.Verified, pending.Status);
            Assert.NotNull(pending.EmailVerifiedAt);
            Assert.True(otp.IsUsed || (await _db.OtpVerifications.SingleAsync()).IsUsed);
            Assert.Empty(await _db.TrialRequests.ToListAsync());
        }

        [Fact]
        public async Task ResendOtpAsync_WithinCooldown_Throws()
        {
            await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "cooldown@example.com",
                    TermsAccepted = true,
                }
            );

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.ResendOtpAsync("cooldown@example.com")
            );
            Assert.Equal("Please wait before resending OTP.", ex.Message);
        }

        [Fact]
        public async Task ResendOtpAsync_MaxResends_SetsAbandoned()
        {
            await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "limit@example.com",
                    TermsAccepted = true,
                }
            );

            var pending = await _db.PendingSignups.SingleAsync();
            pending.LastOtpSentAt = DateTime.UtcNow.AddMinutes(-2);
            pending.OtpResendCount = 5;
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.ResendOtpAsync("limit@example.com")
            );
            Assert.Equal("OTP resend limit reached.", ex.Message);

            pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal(PendingSignupStatuses.Abandoned, pending.Status);
        }

        [Fact]
        public async Task ResendOtpAsync_AfterCooldown_SendsNewOtp()
        {
            await _sut.StartAsync(
                new StartSignupDto
                {
                    Email = "resend@example.com",
                    TermsAccepted = true,
                }
            );

            var pending = await _db.PendingSignups.SingleAsync();
            pending.LastOtpSentAt = DateTime.UtcNow.AddMinutes(-2);
            await _db.SaveChangesAsync();

            await _sut.ResendOtpAsync("resend@example.com");

            Assert.Equal(2, _emailService.SentOtpEmails.Count);
            pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal(1, pending.OtpResendCount);
            Assert.Equal(1, await _db.OtpVerifications.CountAsync(x => !x.IsUsed));
        }

        [Fact]
        public async Task SaveOnboarding_OneLocation_SetsSingle()
        {
            var sessionToken = await SeedVerifiedPendingAsync("single@example.com");

            var result = await _sut.SaveOnboardingAsync(
                sessionToken,
                new SaveSignupOnboardingDto
                {
                    Password = "Password1!",
                    ConfirmPassword = "Password1!",
                    FullName = "Single Owner",
                    GroupName = "Solo Kitchen",
                    BusinessCategory = "takeaway",
                    PrimaryPhone = "+447700900123",
                    Locations =
                    [
                        new SaveSignupOnboardingDto.LocationItem
                        {
                            LocationName = "Main",
                            Address = "1 High St",
                            City = "London",
                            Postcode = "SW1A 1AA",
                        },
                    ],
                }
            );

            // Stub provisioner leaves status at Provisioning (real path reaches Complete).
            Assert.Equal(PendingSignupStatuses.Provisioning, result.Status);
            Assert.Equal(sessionToken, result.SessionToken);

            var pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal("Single", pending.AccountType);
            Assert.Equal(BillingSubscriptionPlans.Free, pending.ChosenPlan);
            Assert.Equal("monthly", pending.ChosenCadence);
            Assert.Equal(PendingSignupStatuses.Provisioning, pending.Status);
            Assert.Equal("Single Owner", pending.FullName);
            Assert.False(string.IsNullOrWhiteSpace(pending.PasswordHash));
            Assert.True(
                BCrypt.Net.BCrypt.Verify("Password1!", pending.PasswordHash)
            );
            Assert.False(string.IsNullOrWhiteSpace(pending.OnboardingJson));
            Assert.DoesNotContain(
                "Password1!",
                pending.OnboardingJson!,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SaveOnboarding_TwoLocations_Throws()
        {
            var sessionToken = await SeedVerifiedPendingAsync("multi@example.com");

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _sut.SaveOnboardingAsync(
                    sessionToken,
                    new SaveSignupOnboardingDto
                    {
                        Password = "Password1!",
                        ConfirmPassword = "Password1!",
                        FullName = "Multi Owner",
                        GroupName = "Group Kitchen",
                        BusinessCategory = "restaurant",
                        Locations =
                        [
                            new SaveSignupOnboardingDto.LocationItem
                            {
                                LocationName = "North",
                                Address = "1 North St",
                                City = "Manchester",
                                Postcode = "M1 1AE",
                            },
                            new SaveSignupOnboardingDto.LocationItem
                            {
                                LocationName = "South",
                                Address = "2 South St",
                                City = "Bristol",
                                Postcode = "BS1 1AA",
                            },
                        ],
                    }
                )
            );

            Assert.Equal("Exactly one location is required.", ex.Message);

            var pending = await _db.PendingSignups.SingleAsync();
            Assert.Null(pending.AccountType);
            Assert.NotEqual("Multi", pending.AccountType);
            Assert.Equal(PendingSignupStatuses.Verified, pending.Status);
        }

        [Fact]
        public async Task SaveOnboarding_EmailPending_Throws()
        {
            var sessionToken = Guid.NewGuid();
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    Email = "early@example.com",
                    Status = PendingSignupStatuses.EmailPending,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                }
            );
            await _db.SaveChangesAsync();

            await Assert.ThrowsAsync<Exception>(() =>
                _sut.SaveOnboardingAsync(
                    sessionToken,
                    new SaveSignupOnboardingDto
                    {
                        Password = "Password1!",
                        ConfirmPassword = "Password1!",
                        FullName = "Too Early",
                        GroupName = "Kitchen",
                        BusinessCategory = "takeaway",
                        Locations =
                        [
                            new SaveSignupOnboardingDto.LocationItem
                            {
                                LocationName = "Main",
                                Address = "1 High St",
                            },
                        ],
                    }
                )
            );
        }

        [Fact]
        public async Task GetBySession_ReturnsResumeWithoutPassword()
        {
            var sessionToken = await SeedVerifiedPendingAsync("resume-get@example.com");

            await _sut.SaveOnboardingAsync(
                sessionToken,
                new SaveSignupOnboardingDto
                {
                    Password = "Password1!",
                    ConfirmPassword = "Password1!",
                    FullName = "Resume Owner",
                    GroupName = "Resume Kitchen",
                    BusinessCategory = "cafe",
                    Locations =
                    [
                        new SaveSignupOnboardingDto.LocationItem
                        {
                            LocationName = "Main",
                            Address = "9 Resume Rd",
                            City = "Leeds",
                            Postcode = "LS1 1AA",
                        },
                    ],
                }
            );

            var resume = await _sut.GetBySessionAsync(sessionToken);

            Assert.Equal(sessionToken, resume.SessionToken);
            Assert.Equal("resume-get@example.com", resume.Email);
            Assert.Equal(PendingSignupStatuses.Provisioning, resume.Status);
            Assert.Equal("provisioning", resume.LastStepHint);
            Assert.Equal("Resume Owner", resume.FullName);
            Assert.Equal("Single", resume.AccountType);
            Assert.Equal("Resume Kitchen", resume.GroupName);
            Assert.Equal("cafe", resume.BusinessCategory);
            Assert.Null(resume.PasswordHash);
            Assert.DoesNotContain(
                "Password1!",
                resume.OnboardingJson ?? "",
                StringComparison.Ordinal
            );
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        private async Task<Guid> SeedVerifiedPendingAsync(string email)
        {
            var sessionToken = Guid.NewGuid();
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    Email = email,
                    Status = PendingSignupStatuses.Verified,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                }
            );
            await _db.SaveChangesAsync();
            return sessionToken;
        }

        private sealed class TrackingEmailService : EmailServiceStubBase
        {
            public List<(string Email, string Otp)> SentOtpEmails { get; } = [];

            public override Task SendOtpEmailAsync(string toEmail, string otp)
            {
                SentOtpEmails.Add((toEmail, otp));
                return Task.CompletedTask;
            }
        }

        private sealed class StubProvisioningService : IProvisioningService
        {
            public Task GenerateActivationCodeAsync(string inviteToken) =>
                Task.CompletedTask;

            public Task ProvisionAsync(
                TummlyBackend.DTOs.Trial.CompleteSetupDto dto
            ) => Task.CompletedTask;

            public Task ProvisionFromPendingAsync(Guid pendingSignupId) =>
                Task.CompletedTask;

            public Task<TummlyBackend.DTOs.Provisioning.InviteTokenResult> ValidateInviteTokenAsync(
                string token
            ) =>
                throw new NotImplementedException();
        }
    }
}
