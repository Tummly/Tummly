using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AdminEnvironmentServiceTests
    {
        private const string RevolutSecret = "sk_live_REVOLUT_SECRET_987";
        private const string TwilioToken = "twilio_auth_token_SECRET_555";
        private const string ResendKey = "re_RESEND_SECRET_321";

        private static readonly DateTime StartedAt = new(2026, 10, 28, 8, 30, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetAsync_NonProductionEnvironment_ReportsNameAndUnknownCommit()
        {
            var dto = await CreateService(environment: "QA").GetAsync();

            Assert.Equal("QA", dto.EnvironmentName);
            Assert.False(dto.IsProduction);
            Assert.Equal("unknown", dto.CommitSha);
            Assert.Equal(StartedAt, dto.ApiStartedAtUtc);
        }

        [Fact]
        public async Task GetAsync_ProductionEnvironment_FlagsProduction()
        {
            var dto = await CreateService(environment: "Production").GetAsync();

            Assert.Equal("Production", dto.EnvironmentName);
            Assert.True(dto.IsProduction);
        }

        [Fact]
        public async Task GetAsync_ReadsCommitShaFromBuildConfig()
        {
            var dto = await CreateService(
                environment: "QA",
                commitSha: "abc1234"
            ).GetAsync();

            Assert.Equal("abc1234", dto.CommitSha);
        }

        [Fact]
        public async Task GetAsync_MissingProviderConfig_IsNotConfigured_NeverHealthy()
        {
            var dto = await CreateService(environment: "QA").GetAsync();

            Assert.Equal("not_configured", dto.Revolut.Status);
            Assert.Equal("unset", dto.Revolut.Mode);
            Assert.Equal("not_configured", dto.Resend.Status);
            Assert.Null(dto.Resend.SenderDomain);
            Assert.Equal("not_configured", dto.Twilio.Status);
            Assert.Null(dto.Twilio.FromNumberMasked);
            Assert.False(dto.VatModeActive);
        }

        [Fact]
        public async Task GetAsync_ConfiguredProviders_ReportModeDomainAndMaskedNumber()
        {
            var dto = await CreateService(
                environment: "QA",
                revolut: new RevolutSettings
                {
                    SecretKey = RevolutSecret,
                    ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                    ApiVersion = "2024-09-01",
                },
                email: new EmailSettings
                {
                    ApiKey = ResendKey,
                    SenderEmail = "no-reply@tummly.com",
                },
                twilio: new TwilioSettings
                {
                    AccountSid = "AC123",
                    AuthToken = TwilioToken,
                    VerifyServiceSid = "VA123",
                    RecoveryFromNumber = "+447700900123",
                },
                vat: new TummlySellerVatSettings { IsActive = true }
            ).GetAsync();

            Assert.Equal("configured", dto.Revolut.Status);
            Assert.Equal("sandbox", dto.Revolut.Mode);
            Assert.Equal("configured", dto.Resend.Status);
            Assert.Equal("tummly.com", dto.Resend.SenderDomain);
            Assert.Equal("configured", dto.Twilio.Status);
            Assert.Equal("+********0123", dto.Twilio.FromNumberMasked);
            Assert.True(dto.VatModeActive);
        }

        [Fact]
        public async Task GetAsync_NeverSerialisesSecrets()
        {
            var dto = await CreateService(
                environment: "QA",
                revolut: new RevolutSettings
                {
                    SecretKey = RevolutSecret,
                    ApiBaseUrl = RevolutSettings.LiveApiBaseUrl,
                    ApiVersion = "2024-09-01",
                },
                email: new EmailSettings { ApiKey = ResendKey, SenderEmail = "no-reply@tummly.com" },
                twilio: new TwilioSettings
                {
                    AccountSid = "AC123",
                    AuthToken = TwilioToken,
                    VerifyServiceSid = "VA123",
                    RecoveryFromNumber = "+447700900123",
                }
            ).GetAsync();

            var json = JsonSerializer.Serialize(dto);

            Assert.DoesNotContain(RevolutSecret, json);
            Assert.DoesNotContain(TwilioToken, json);
            Assert.DoesNotContain(ResendKey, json);
            Assert.Equal("live", dto.Revolut.Mode);
        }

        private static AdminEnvironmentService CreateService(
            string environment,
            string? commitSha = null,
            RevolutSettings? revolut = null,
            EmailSettings? email = null,
            TwilioSettings? twilio = null,
            TummlySellerVatSettings? vat = null
        )
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            var db = new ApplicationDbContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Build:CommitSha"] = commitSha }
                )
                .Build();

            return new AdminEnvironmentService(
                db,
                new TestHostEnvironment(environment),
                configuration,
                new ApiStartedAtUtc(StartedAt),
                Options.Create(revolut ?? new RevolutSettings()),
                Options.Create(email ?? new EmailSettings()),
                Options.Create(twilio ?? new TwilioSettings()),
                Options.Create(vat ?? new TummlySellerVatSettings())
            );
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public TestHostEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "TummlyBackend";

            public string ContentRootPath { get; set; } = string.Empty;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
