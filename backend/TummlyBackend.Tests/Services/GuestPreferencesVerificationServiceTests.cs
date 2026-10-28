using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="GuestPreferencesVerificationService"/> — contact proof for the guest
    /// preference centre (email confirm link, UK mobile code).
    /// </summary>
    public class GuestPreferencesVerificationServiceTests : IDisposable
    {
        private const string HashKey = "test-guest-preferences-hash-key";
        private const string FrontendBaseUrl = "https://tummly.example";
        private const string KnownEmail = "known@example.com";
        private const string KnownPhone = "+447911123456";
        private const string KnownPhoneDigits = "447911123456";

        private readonly ApplicationDbContext _db;
        private readonly FakeTimeProvider _time;
        private readonly FakeSmsService _sms;
        private readonly CapturingEmailService _email;
        private readonly GuestPreferencesVerificationService _sut;

        public GuestPreferencesVerificationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero));
            _sms = new FakeSmsService();
            _email = new CapturingEmailService();

            var hasher = new OtpCodeHasher(HashKey);
            var otp = new OtpChallengeService(_db, hasher, _time);
            var identity = new TummlyGuestIdentityService(_db);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Frontend:BaseUrl"] = FrontendBaseUrl }
                )
                .Build();

            _sut = new GuestPreferencesVerificationService(
                _db,
                otp,
                hasher,
                _sms,
                _email,
                identity,
                configuration,
                _time,
                NullLogger<GuestPreferencesVerificationService>.Instance
            );

            SeedMasterGuest();
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        [Fact]
        public async Task Start_UnknownEmail_SendsNothing()
        {
            await _sut.StartAsync("nobody@example.com", null, CancellationToken.None);

            Assert.Empty(_email.Sent);
            Assert.Empty(await _db.OtpVerifications.ToListAsync());
        }

        [Fact]
        public async Task Start_KnownEmail_SendsLink()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);

            var sent = Assert.Single(_email.Sent);
            Assert.Equal(KnownEmail, sent.ToEmail);
            Assert.StartsWith($"{FrontendBaseUrl}/preferences?v=", sent.ConfirmUrl);
            Assert.DoesNotContain(KnownEmail, sent.ConfirmUrl);
        }

        [Fact]
        public async Task Start_InvalidContact_SendsNothing()
        {
            await _sut.StartAsync("not-a-contact", null, CancellationToken.None);

            Assert.Empty(_email.Sent);
            Assert.Empty(_sms.SentPhones);
        }

        [Fact]
        public async Task Start_SixthInHour_SendsNothing()
        {
            for (var i = 0; i < GuestPreferencesVerificationService.MaxStartsPerContactPerHour; i++)
            {
                await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            }

            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);

            Assert.Equal(
                GuestPreferencesVerificationService.MaxStartsPerContactPerHour,
                _email.Sent.Count
            );
        }

        [Fact]
        public async Task Start_AfterAnHour_SendsAgain()
        {
            for (var i = 0; i < GuestPreferencesVerificationService.MaxStartsPerContactPerHour; i++)
            {
                await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            }

            _time.Advance(TimeSpan.FromHours(1) + TimeSpan.FromSeconds(1));
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);

            Assert.Equal(
                GuestPreferencesVerificationService.MaxStartsPerContactPerHour + 1,
                _email.Sent.Count
            );
        }

        [Fact]
        public async Task Start_AddContact_SendsWithoutMasterGuest()
        {
            var owner = await FindOrCreateOwnerAsync();

            await _sut.StartAsync("new-contact@example.com", owner.TummlyGuestId, CancellationToken.None);

            var sent = Assert.Single(_email.Sent);
            Assert.StartsWith($"{FrontendBaseUrl}/preferences?link=", sent.ConfirmUrl);
        }

        [Fact]
        public async Task VerifyEmailLink_Ok_CreatesGuest()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);

            var result = await _sut.VerifyEmailLinkAsync(LinkParameter(), CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Ok, result.Outcome);
            Assert.NotNull(result.Proven);
            var contact = await _db.TummlyGuestContacts.SingleAsync();
            Assert.Equal(TummlyGuestContactTypes.Email, contact.ContactType);
            Assert.Equal(KnownEmail, contact.NormalizedValue);
            Assert.Equal(result.Proven!.TummlyGuestId, contact.TummlyGuestId);
            Assert.Equal(result.Proven.ContactId, contact.Id);
            Assert.Equal(1, await _db.TummlyGuests.CountAsync());
        }

        [Fact]
        public async Task VerifyEmailLink_Reused_Invalid()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            var link = LinkParameter();
            var first = await _sut.VerifyEmailLinkAsync(link, CancellationToken.None);
            Assert.Equal(GuestVerifyOutcome.Ok, first.Outcome);

            var second = await _sut.VerifyEmailLinkAsync(link, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, second.Outcome);
            Assert.Null(second.Proven);
        }

        [Fact]
        public async Task VerifyEmailLink_WrongCode_Invalid()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            var link = LinkParameter();
            var dot = link.IndexOf('.');
            var code = link[(dot + 1)..];
            var wrong = link[..(dot + 1)] + (code == "000000" ? "111111" : "000000");

            var result = await _sut.VerifyEmailLinkAsync(wrong, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, result.Outcome);
            Assert.Empty(await _db.TummlyGuests.ToListAsync());
        }

        [Fact]
        public async Task VerifyEmailLink_AfterTenMinutes_Expired()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            var link = LinkParameter();

            _time.Advance(TimeSpan.FromMinutes(10));
            var result = await _sut.VerifyEmailLinkAsync(link, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Expired, result.Outcome);
        }

        [Fact]
        public async Task VerifyEmailLink_AddContact_LinksToOwner()
        {
            var owner = await FindOrCreateOwnerAsync();
            await _sut.StartAsync("new-contact@example.com", owner.TummlyGuestId, CancellationToken.None);

            var result = await _sut.VerifyEmailLinkAsync(LinkParameter(), CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Ok, result.Outcome);
            Assert.Equal(owner.TummlyGuestId, result.Proven!.TummlyGuestId);
            var linked = await _db.TummlyGuestContacts
                .SingleAsync(c => c.NormalizedValue == "new-contact@example.com");
            Assert.Equal(owner.TummlyGuestId, linked.TummlyGuestId);
            Assert.Equal(result.Proven.ContactId, linked.Id);
        }

        [Fact]
        public async Task VerifyEmailLink_AddContact_MissingOwner_Invalid()
        {
            await _sut.StartAsync("orphan-owner@example.com", 987654, CancellationToken.None);

            var result = await _sut.VerifyEmailLinkAsync(LinkParameter(), CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, result.Outcome);
            Assert.Null(result.Proven);
            Assert.Empty(await _db.TummlyGuestContacts.ToListAsync());
        }

        [Fact]
        public async Task VerifyCode_WrongCode_Invalid()
        {
            _sms.VerifyResult = false;

            var result = await _sut.VerifyCodeAsync(KnownPhone, "123456", null, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, result.Outcome);
            Assert.Null(result.Proven);
            Assert.Empty(await _db.TummlyGuests.ToListAsync());
        }

        [Fact]
        public async Task VerifyCode_RightCode_CreatesGuest()
        {
            await _sut.StartAsync(KnownPhone, null, CancellationToken.None);
            _sms.VerifyResult = true;

            var result = await _sut.VerifyCodeAsync(KnownPhone, "123456", null, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Ok, result.Outcome);
            Assert.Equal(
                new[] { KnownPhone },
                _sms.SentPhones
            );
            var contact = await _db.TummlyGuestContacts.SingleAsync();
            Assert.Equal(TummlyGuestContactTypes.Phone, contact.ContactType);
            Assert.Equal(KnownPhone, contact.NormalizedValue);
        }

        [Fact]
        public async Task VerifyCode_EmailContact_Invalid()
        {
            _sms.VerifyResult = true;

            var result = await _sut.VerifyCodeAsync(KnownEmail, "123456", null, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, result.Outcome);
            Assert.Empty(_sms.Checked);
        }

        [Fact]
        public async Task Start_KnownPhone_RecordsStartForRateLimit()
        {
            for (var i = 0; i < GuestPreferencesVerificationService.MaxStartsPerContactPerHour + 2; i++)
            {
                await _sut.StartAsync(KnownPhone, null, CancellationToken.None);
            }

            Assert.Equal(
                GuestPreferencesVerificationService.MaxStartsPerContactPerHour,
                _sms.SentPhones.Count
            );
        }

        private string LinkParameter()
        {
            var sent = _email.Sent.Last().ConfirmUrl;
            var query = sent[(sent.IndexOf('?') + 1)..];
            return query.Split('=', 2)[1];
        }

        private async Task<ProvenContact> FindOrCreateOwnerAsync()
        {
            var identity = new TummlyGuestIdentityService(_db);
            return await identity.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Phone,
                KnownPhone,
                CancellationToken.None
            );
        }

        private void SeedMasterGuest()
        {
            _db.MasterGuests.Add(
                new MasterGuest
                {
                    RestaurantId = 1,
                    Email = KnownEmail,
                    NormalizedEmail = KnownEmail,
                    Mobile = KnownPhone,
                    NormalizedPhone = KnownPhoneDigits,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            _db.SaveChanges();
        }

        private sealed class FakeSmsService : ISmsService
        {
            public List<string> SentPhones { get; } = new();

            public List<string> Checked { get; } = new();

            public bool VerifyResult { get; set; }

            public Task SendOtpSmsAsync(string phoneNumber)
            {
                SentPhones.Add(phoneNumber);
                return Task.CompletedTask;
            }

            public Task<bool> VerifyOtpSmsAsync(string phoneNumber, string otp)
            {
                Checked.Add(phoneNumber);
                return Task.FromResult(VerifyResult);
            }
        }

        private sealed class CapturingEmailService : EmailServiceStubBase
        {
            public List<(string ToEmail, string ConfirmUrl)> Sent { get; } = new();

            public override Task SendGuestPreferencesConfirmEmailAsync(
                string toEmail,
                string confirmUrl
            )
            {
                Sent.Add((toEmail, confirmUrl));
                return Task.CompletedTask;
            }
        }

        private sealed class FakeTimeProvider : TimeProvider
        {
            private DateTimeOffset _now;

            public FakeTimeProvider(DateTimeOffset start)
            {
                _now = start;
            }

            public override DateTimeOffset GetUtcNow() => _now;

            public void Advance(TimeSpan by)
            {
                _now = _now.Add(by);
            }
        }
    }
}
