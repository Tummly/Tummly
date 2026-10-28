using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    /// preference centre. Provider sends run in <see cref="GuestPreferencesSendWorker"/>,
    /// drained here with <c>DrainPendingAsync</c>.
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
        private readonly GuestPreferencesSendWorker _worker;
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

            var services = new ServiceCollection();
            services.AddSingleton<IEmailService>(_email);
            services.AddSingleton<ISmsService>(_sms);
            var root = services.BuildServiceProvider();
            _worker = new GuestPreferencesSendWorker(
                root.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<GuestPreferencesSendWorker>.Instance
            );

            _sut = BuildService(FrontendBaseUrl);
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
            await DrainAsync();

            Assert.Empty(_email.Sent);
            Assert.Empty(await _db.OtpVerifications.ToListAsync());
        }

        [Fact]
        public async Task Start_KnownEmail_SendsLink()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            await DrainAsync();

            var sent = Assert.Single(_email.Sent);
            Assert.False(sent.IsAddContact);
            Assert.Equal(KnownEmail, sent.ToEmail);
            Assert.StartsWith($"{FrontendBaseUrl}/preferences?v=", sent.ConfirmUrl);
            Assert.DoesNotContain(KnownEmail, sent.ConfirmUrl);
        }

        [Fact]
        public async Task Start_InvalidContact_SendsNothing()
        {
            await _sut.StartAsync("not-a-contact", null, CancellationToken.None);
            await DrainAsync();

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
            await DrainAsync();

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
            await DrainAsync();

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
            await DrainAsync();

            var sent = Assert.Single(_email.Sent);
            Assert.True(sent.IsAddContact);
            Assert.StartsWith($"{FrontendBaseUrl}/preferences?link=", sent.ConfirmUrl);
        }

        [Fact]
        public async Task Start_ProviderBlocked_StartCompletes_SendHappensOnDrain()
        {
            _email.Gate = new TaskCompletionSource();
            var start = _sut.StartAsync(KnownEmail, null, CancellationToken.None);

            // The request finishes while the provider would still be blocked: it never calls it.
            await start.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(_email.Entered.Task.IsCompleted);
            Assert.Empty(_email.Sent);

            var drain = _worker.DrainPendingAsync(CancellationToken.None);
            await _email.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(drain.IsCompleted);

            _email.Gate.SetResult();
            await drain.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Single(_email.Sent);
        }

        [Fact]
        public async Task Send_ProviderFails_IsSwallowed()
        {
            _email.Fail = true;
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);

            await _worker.DrainPendingAsync(CancellationToken.None);

            Assert.Empty(_email.Sent);
        }

        [Fact]
        public async Task Run_ProviderTimesOut_LoopKeepsSendingNextItem()
        {
            using var cts = new CancellationTokenSource();
            _email.CancelNextSend = true;
            var loop = _worker.RunAsync(cts.Token);

            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (_email.Sent.Count == 0 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(10);
            }

            Assert.False(loop.IsCompleted);
            Assert.Single(_email.Sent);

            cts.Cancel();
            await loop.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(loop.IsCompletedSuccessfully);
        }

        [Fact]
        public void TryEnqueue_WhenFull_ReturnsFalse()
        {
            var item = new GuestPreferencesSendItem(TummlyGuestContactTypes.Phone, KnownPhone, null);
            for (var i = 0; i < GuestPreferencesSendWorker.Capacity; i++)
            {
                Assert.True(_worker.TryEnqueue(item));
            }

            Assert.False(_worker.TryEnqueue(item));
        }

        [Fact]
        public async Task Start_MissingFrontendBaseUrl_WritesNothing_ForKnownAndUnknown()
        {
            var misconfigured = BuildService(frontendBaseUrl: null);

            await misconfigured.StartAsync(KnownEmail, null, CancellationToken.None);
            await misconfigured.StartAsync("nobody@example.com", null, CancellationToken.None);
            await DrainAsync();

            Assert.Empty(await _db.OtpVerifications.ToListAsync());
            Assert.Empty(_email.Sent);
        }

        [Fact]
        public async Task VerifyEntryEmailLink_Ok_CreatesGuest()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);

            var result = await _sut.VerifyEntryEmailLinkAsync(await LinkParameterAsync(), CancellationToken.None);

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
        public async Task VerifyEntryEmailLink_Reused_Invalid()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            var link = await LinkParameterAsync();
            var first = await _sut.VerifyEntryEmailLinkAsync(link, CancellationToken.None);
            Assert.Equal(GuestVerifyOutcome.Ok, first.Outcome);

            var second = await _sut.VerifyEntryEmailLinkAsync(link, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, second.Outcome);
            Assert.Null(second.Proven);
        }

        [Fact]
        public async Task VerifyEntryEmailLink_WrongCode_Invalid()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            var link = await LinkParameterAsync();
            var dot = link.IndexOf('.');
            var code = link[(dot + 1)..];
            var wrong = link[..(dot + 1)] + (code == "000000" ? "111111" : "000000");

            var result = await _sut.VerifyEntryEmailLinkAsync(wrong, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, result.Outcome);
            Assert.Empty(await _db.TummlyGuests.ToListAsync());
        }

        [Fact]
        public async Task VerifyEntryEmailLink_AfterTenMinutes_Expired()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            var link = await LinkParameterAsync();

            _time.Advance(TimeSpan.FromMinutes(10));
            var result = await _sut.VerifyEntryEmailLinkAsync(link, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Expired, result.Outcome);
        }

        [Fact]
        public async Task VerifyAddContactEmailLink_LinksToOwner()
        {
            var owner = await FindOrCreateOwnerAsync();
            await _sut.StartAsync("new-contact@example.com", owner.TummlyGuestId, CancellationToken.None);

            var result = await _sut.VerifyAddContactEmailLinkAsync(await LinkParameterAsync(), CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Ok, result.Outcome);
            Assert.Equal(owner.TummlyGuestId, result.Proven!.TummlyGuestId);
            var linked = await _db.TummlyGuestContacts
                .SingleAsync(c => c.NormalizedValue == "new-contact@example.com");
            Assert.Equal(owner.TummlyGuestId, linked.TummlyGuestId);
            Assert.Equal(result.Proven.ContactId, linked.Id);
        }

        [Fact]
        public async Task VerifyAddContactEmailLink_MissingOwner_Invalid_NotConsumed()
        {
            await _sut.StartAsync("orphan-owner@example.com", 987654, CancellationToken.None);
            var link = await LinkParameterAsync();

            var result = await _sut.VerifyAddContactEmailLinkAsync(link, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, result.Outcome);
            Assert.Null(result.Proven);
            Assert.Empty(await _db.TummlyGuestContacts.ToListAsync());
            var row = await _db.OtpVerifications.SingleAsync(r => r.SubjectId == "987654");
            Assert.False(row.IsUsed);
        }

        [Fact]
        public async Task VerifyEntry_AddContactRow_Invalid_NotConsumed()
        {
            var owner = await FindOrCreateOwnerAsync();
            await _sut.StartAsync("wrong-flow@example.com", owner.TummlyGuestId, CancellationToken.None);
            var link = await LinkParameterAsync();

            var wrongFlow = await _sut.VerifyEntryEmailLinkAsync(link, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, wrongFlow.Outcome);
            Assert.Null(wrongFlow.Proven);
            Assert.DoesNotContain(
                await _db.TummlyGuestContacts.ToListAsync(),
                c => c.NormalizedValue == "wrong-flow@example.com"
            );

            var linked = await _sut.VerifyAddContactEmailLinkAsync(link, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Ok, linked.Outcome);
            Assert.Equal(owner.TummlyGuestId, linked.Proven!.TummlyGuestId);
        }

        [Fact]
        public async Task VerifyAddContact_EntryRow_Invalid_NotConsumed()
        {
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            var link = await LinkParameterAsync();

            var wrongFlow = await _sut.VerifyAddContactEmailLinkAsync(link, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Invalid, wrongFlow.Outcome);
            Assert.Null(wrongFlow.Proven);

            var entry = await _sut.VerifyEntryEmailLinkAsync(link, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Ok, entry.Outcome);
        }

        [Fact]
        public async Task AddContactStart_DoesNotInvalidate_PendingEntryLink()
        {
            var owner = await FindOrCreateOwnerAsync();
            await _sut.StartAsync(KnownEmail, null, CancellationToken.None);
            await _sut.StartAsync(KnownEmail, owner.TummlyGuestId, CancellationToken.None);
            await DrainAsync();

            var entryLink = _email.Sent.Single(s => s.ConfirmUrl.Contains("?v=")).ConfirmUrl;
            var entryParameter = entryLink[(entryLink.IndexOf("?v=", StringComparison.Ordinal) + 3)..];

            var result = await _sut.VerifyEntryEmailLinkAsync(entryParameter, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Ok, result.Outcome);
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
            await DrainAsync();
            _sms.VerifyResult = true;

            var result = await _sut.VerifyCodeAsync(KnownPhone, "123456", null, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Ok, result.Outcome);
            Assert.Equal(new[] { KnownPhone }, _sms.SentPhones);
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
            await DrainAsync();

            Assert.Equal(
                GuestPreferencesVerificationService.MaxStartsPerContactPerHour,
                _sms.SentPhones.Count
            );
        }

        private GuestPreferencesVerificationService BuildService(string? frontendBaseUrl)
        {
            var hasher = new OtpCodeHasher(HashKey);
            var otp = new OtpChallengeService(_db, hasher, _time);
            var identity = new TummlyGuestIdentityService(_db);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Frontend:BaseUrl"] = frontendBaseUrl }
                )
                .Build();

            return new GuestPreferencesVerificationService(
                _db,
                otp,
                hasher,
                _sms,
                _worker,
                identity,
                configuration,
                _time,
                NullLogger<GuestPreferencesVerificationService>.Instance
            );
        }

        /// <summary>Sends everything queued so far. The request path only enqueues.</summary>
        private Task DrainAsync() => _worker.DrainPendingAsync(CancellationToken.None);

        private async Task<string> LinkParameterAsync()
        {
            await DrainAsync();
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
            private readonly object _lock = new();
            private readonly List<(string ToEmail, string ConfirmUrl, bool IsAddContact)> _sent = new();

            /// <summary>When set, the send waits here before it records anything.</summary>
            public TaskCompletionSource? Gate { get; set; }

            /// <summary>Completes when a send enters the provider call.</summary>
            public TaskCompletionSource Entered { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool Fail { get; set; }

            /// <summary>When set, the next send throws a non-shutdown TaskCanceledException once.</summary>
            public bool CancelNextSend { get; set; }

            public IReadOnlyList<(string ToEmail, string ConfirmUrl, bool IsAddContact)> Sent
            {
                get
                {
                    lock (_lock)
                    {
                        return _sent.ToList();
                    }
                }
            }

            public override Task SendGuestPreferencesAddContactConfirmEmailAsync(
                string toEmail,
                string confirmUrl
            )
            {
                lock (_lock)
                {
                    _sent.Add((toEmail, confirmUrl, true));
                }

                return Task.CompletedTask;
            }

            public override async Task SendGuestPreferencesConfirmEmailAsync(
                string toEmail,
                string confirmUrl
            )
            {
                Entered.TrySetResult();
                if (Gate is not null)
                {
                    await Gate.Task;
                }

                if (CancelNextSend)
                {
                    CancelNextSend = false;
                    throw new TaskCanceledException("provider timeout");
                }

                if (Fail)
                {
                    throw new InvalidOperationException("provider down");
                }

                lock (_lock)
                {
                    _sent.Add((toEmail, confirmUrl, false));
                }
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
