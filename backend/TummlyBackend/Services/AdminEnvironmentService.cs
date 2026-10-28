using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>Process start time of this API instance (UTC).</summary>
    public sealed record ApiStartedAtUtc(DateTime Value);

    public sealed class AdminEnvironmentService : IAdminEnvironmentService
    {
        private const string UnknownCommit = "unknown";
        private const string Configured = "configured";
        private const string NotConfigured = "not_configured";
        private const int VisibleDigits = 4;

        private readonly ApplicationDbContext _context;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly DateTime _apiStartedAtUtc;
        private readonly RevolutSettings _revolut;
        private readonly EmailSettings _email;
        private readonly TwilioSettings _twilio;
        private readonly TummlySellerVatSettings _vat;

        public AdminEnvironmentService(
            ApplicationDbContext context,
            IHostEnvironment hostEnvironment,
            IConfiguration configuration,
            ApiStartedAtUtc apiStartedAt,
            IOptions<RevolutSettings> revolut,
            IOptions<EmailSettings> email,
            IOptions<TwilioSettings> twilio,
            IOptions<TummlySellerVatSettings> vat
        )
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _apiStartedAtUtc = apiStartedAt.Value;
            _revolut = revolut.Value;
            _email = email.Value;
            _twilio = twilio.Value;
            _vat = vat.Value;
        }

        public async Task<AdminEnvironmentDto> GetAsync(
            CancellationToken cancellationToken = default
        )
        {
            return new AdminEnvironmentDto
            {
                EnvironmentName = _hostEnvironment.EnvironmentName,
                IsProduction = _hostEnvironment.IsProduction(),
                CommitSha = CommitSha(),
                ApiStartedAtUtc = _apiStartedAtUtc,
                LatestMigrationId = await LatestAppliedMigrationAsync(cancellationToken),
                Revolut = new AdminRevolutReadinessDto
                {
                    Status = _revolut.HasMerchantApiConfig ? Configured : NotConfigured,
                    Mode = _revolut.HostMode,
                },
                Resend = new AdminResendReadinessDto
                {
                    Status = HasValue(_email.ApiKey) && HasValue(_email.SenderEmail)
                        ? Configured
                        : NotConfigured,
                    SenderDomain = SenderDomain(_email.SenderEmail),
                },
                Twilio = new AdminTwilioReadinessDto
                {
                    Status = _twilio.IsConfigured && HasValue(_twilio.RecoveryFromNumber)
                        ? Configured
                        : NotConfigured,
                    FromNumberMasked = MaskNumber(_twilio.RecoveryFromNumber),
                },
                VatModeActive = _vat.IsActive,
            };
        }

        /// <summary>
        /// Keeps the last four digits and masks the rest, for example
        /// "+447700900123" becomes "+********0123". Values with four or fewer
        /// digits are fully masked.
        /// </summary>
        public static string? MaskNumber(string? number)
        {
            if (!HasValue(number))
            {
                return null;
            }

            var trimmed = number!.Trim();
            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (digits.Length <= VisibleDigits)
            {
                return "****";
            }

            var prefix = trimmed.StartsWith('+') ? "+" : string.Empty;
            return $"{prefix}{new string('*', digits.Length - VisibleDigits)}{digits[^VisibleDigits..]}";
        }

        private string CommitSha()
        {
            var value = _configuration["Build:CommitSha"];
            return HasValue(value) ? value!.Trim() : UnknownCommit;
        }

        private async Task<string?> LatestAppliedMigrationAsync(
            CancellationToken cancellationToken
        )
        {
            try
            {
                var applied = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);
                return applied.LastOrDefault();
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }

        private static string? SenderDomain(string? senderEmail)
        {
            if (!HasValue(senderEmail))
            {
                return null;
            }

            var at = senderEmail!.LastIndexOf('@');
            if (at < 0 || at == senderEmail.Length - 1)
            {
                return null;
            }

            return senderEmail[(at + 1)..].Trim().ToLowerInvariant();
        }

        private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);
    }
}
