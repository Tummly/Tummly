using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Proves a contact for the guest preference centre (spec §6). Email proof is a
    /// single-use confirm link. UK mobile proof is a Twilio Verify code. Both count starts
    /// per contact in <see cref="OtpVerification"/> rows.
    /// </summary>
    public sealed class GuestPreferencesVerificationService : IGuestPreferencesVerificationService
    {
        /// <summary>Subject of an entry challenge (no session). Add-contact rows use the owner id.</summary>
        public const string EntrySubjectId = "entry";

        public const int MaxStartsPerContactPerHour = 5;

        public static readonly TimeSpan StartWindow = TimeSpan.FromHours(1);

        private const string EntryLinkParameter = "v";

        private const string AddContactLinkParameter = "link";

        private readonly ApplicationDbContext _context;
        private readonly IOtpChallengeService _otp;
        private readonly IOtpCodeHasher _hasher;
        private readonly ISmsService _sms;
        private readonly IEmailService _email;
        private readonly ITummlyGuestIdentityService _identity;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<GuestPreferencesVerificationService> _logger;

        public GuestPreferencesVerificationService(
            ApplicationDbContext context,
            IOtpChallengeService otp,
            IOtpCodeHasher hasher,
            ISmsService sms,
            IEmailService email,
            ITummlyGuestIdentityService identity,
            IConfiguration configuration,
            TimeProvider timeProvider,
            ILogger<GuestPreferencesVerificationService> logger
        )
        {
            _context = context;
            _otp = otp;
            _hasher = hasher;
            _sms = sms;
            _email = email;
            _identity = identity;
            _configuration = configuration;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task StartAsync(
            string rawContact,
            int? linkToTummlyGuestId,
            CancellationToken ct
        )
        {
            if (
                !GuestPreferenceContact.TryParse(
                    rawContact,
                    out var contactType,
                    out var value
                )
            )
            {
                return;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var channel = contactType == TummlyGuestContactTypes.Email
                ? OtpVerification.ChannelEmail
                : OtpVerification.ChannelSms;

            if (await CountRecentStartsAsync(value, channel, now, ct) >= MaxStartsPerContactPerHour)
            {
                return;
            }

            if (
                linkToTummlyGuestId is null
                && !await MatchesMasterGuestAsync(contactType, value, ct)
            )
            {
                return;
            }

            var subjectId = linkToTummlyGuestId?.ToString(CultureInfo.InvariantCulture)
                ?? EntrySubjectId;

            if (contactType == TummlyGuestContactTypes.Email)
            {
                await SendEmailLinkAsync(value, subjectId, linkToTummlyGuestId, now, ct);
            }
            else
            {
                await SendSmsCodeAsync(value, subjectId, now, ct);
            }
        }

        public async Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyCodeAsync(
            string rawContact,
            string code,
            int? linkToTummlyGuestId,
            CancellationToken ct
        )
        {
            if (
                !GuestPreferenceContact.TryParse(
                    rawContact,
                    out var contactType,
                    out var value
                )
                || contactType != TummlyGuestContactTypes.Phone
            )
            {
                return Invalid();
            }

            var submitted = code?.Trim() ?? string.Empty;
            if (submitted.Length == 0)
            {
                return Invalid();
            }

            if (!await _sms.VerifyOtpSmsAsync(value, submitted))
            {
                return Invalid();
            }

            return await ProveContactAsync(
                contactType,
                value,
                linkToTummlyGuestId,
                ct
            );
        }

        public Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyEntryEmailLinkAsync(
            string v,
            CancellationToken ct
        ) => VerifyEmailLinkAsync(v, expectEntry: true, ct);

        public Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyAddContactEmailLinkAsync(
            string v,
            CancellationToken ct
        ) => VerifyEmailLinkAsync(v, expectEntry: false, ct);

        /// <summary>
        /// Shared email link check. A row of the other flow is Invalid before the code is
        /// checked, so the wrong endpoint never consumes the link.
        /// </summary>
        private async Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> VerifyEmailLinkAsync(
            string v,
            bool expectEntry,
            CancellationToken ct
        )
        {
            if (!TryParseEmailLink(v, out var challengeId, out var code))
            {
                return Invalid();
            }

            var challenge = await _context.OtpVerifications
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == challengeId, ct);

            if (
                challenge is null
                || challenge.Purpose != OtpPurposes.GuestPreferences
                || challenge.Channel != OtpVerification.ChannelEmail
                || challenge.Email is null
                || challenge.SubjectId is null
                || challenge.IsUsed
            )
            {
                return Invalid();
            }

            var isEntry = challenge.SubjectId == EntrySubjectId;
            if (isEntry != expectEntry)
            {
                return Invalid();
            }

            // For add-contact the subject is the owner id. Check it before the code is consumed.
            var ownerId = 0;
            if (
                !isEntry
                && !int.TryParse(
                    challenge.SubjectId,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out ownerId
                )
            )
            {
                return Invalid();
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (challenge.ExpiresAt <= now)
            {
                return Expired();
            }

            var verified = await VerifyEmailChallengeAsync(challenge, code, ct);
            if (verified != GuestVerifyOutcome.Ok)
            {
                return (verified, null);
            }

            if (isEntry)
            {
                var proven = await _identity.FindOrCreateByContactAsync(
                    TummlyGuestContactTypes.Email,
                    challenge.Email,
                    ct
                );
                return (GuestVerifyOutcome.Ok, proven);
            }

            return await ProveContactAsync(
                TummlyGuestContactTypes.Email,
                challenge.Email,
                ownerId,
                ct
            );
        }

        private async Task<(GuestVerifyOutcome Outcome, ProvenContact? Proven)> ProveContactAsync(
            string contactType,
            string value,
            int? linkToTummlyGuestId,
            CancellationToken ct
        )
        {
            if (linkToTummlyGuestId is not int ownerId)
            {
                var proven = await _identity.FindOrCreateByContactAsync(
                    contactType,
                    value,
                    ct
                );
                return (GuestVerifyOutcome.Ok, proven);
            }

            // LinkContactAsync throws when the owner is gone, so check first and report Invalid.
            var ownerExists = await _context.TummlyGuests.AnyAsync(
                guest => guest.Id == ownerId,
                ct
            );
            if (!ownerExists)
            {
                return Invalid();
            }

            var contactId = await _identity.LinkContactAsync(
                ownerId,
                contactType,
                value,
                ct
            );
            return (GuestVerifyOutcome.Ok, new ProvenContact(ownerId, contactId));
        }

        /// <summary>
        /// Runs the OTP check for an email challenge. The OTP service compares the newest
        /// unused challenge for this address, which is this row, because older rows were
        /// marked used. A successful check is single-use and wrong guesses are counted.
        /// </summary>
        private async Task<GuestVerifyOutcome> VerifyEmailChallengeAsync(
            OtpVerification challenge,
            string code,
            CancellationToken ct
        )
        {
            var result = await _otp.VerifyAsync(
                new OtpVerifyRequest(
                    challenge.Email!,
                    challenge.Purpose!,
                    challenge.SubjectId!,
                    challenge.Generation,
                    code
                ),
                ct
            );

            return result.Outcome switch
            {
                OtpVerifyOutcome.Verified => GuestVerifyOutcome.Ok,
                OtpVerifyOutcome.Expired => GuestVerifyOutcome.Expired,
                _ => GuestVerifyOutcome.Invalid,
            };
        }

        private async Task SendEmailLinkAsync(
            string email,
            string subjectId,
            int? linkToTummlyGuestId,
            DateTime now,
            CancellationToken ct
        )
        {
            var code = GenerateOtp.CreateOtp();
            var challenge = await IssueEmailChallengeAsync(email, subjectId, code, now, ct);

            var parameter = linkToTummlyGuestId is null
                ? EntryLinkParameter
                : AddContactLinkParameter;
            var confirmUrl =
                $"{FrontendBaseUrl()}/preferences?{parameter}={challenge.Id}.{code}";

            try
            {
                await _email.SendGuestPreferencesConfirmEmailAsync(email, confirmUrl);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Same response either way, so a provider failure never shows whether the contact is on file.
                _logger.LogProviderSendFailure(
                    ex,
                    "Guest preferences confirm email failed for {MaskedEmail}",
                    LogContactMask.Email(email)
                );
            }
        }

        private async Task SendSmsCodeAsync(
            string phone,
            string subjectId,
            DateTime now,
            CancellationToken ct
        )
        {
            // The code lives at Twilio Verify. This row only counts the start.
            _context.OtpVerifications.Add(
                new OtpVerification
                {
                    Email = phone,
                    OtpCode = OtpVerification.TwilioManagedCode,
                    Channel = OtpVerification.ChannelSms,
                    IsUsed = false,
                    ExpiresAt = now.Add(OtpChallengeService.CodeLifetime),
                    CreatedAt = now,
                    Purpose = OtpPurposes.GuestPreferences,
                    SubjectId = subjectId,
                    Generation = 0,
                    FailedAttempts = 0,
                }
            );
            await _context.SaveChangesAsync(ct);

            try
            {
                await _sms.SendOtpSmsAsync(phone);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogProviderSendFailure(
                    ex,
                    "Guest preferences SMS code failed for {MaskedPhone}",
                    LogContactMask.Phone(phone)
                );
            }
        }

        /// <summary>
        /// Adds a new email challenge and marks older unused challenges for the same
        /// address as used. Rows are kept, unlike <see cref="OtpChallengeService.IssueAsync"/>,
        /// which deletes them. The start count reads these rows.
        /// </summary>
        private async Task<OtpVerification> IssueEmailChallengeAsync(
            string email,
            string subjectId,
            string code,
            DateTime now,
            CancellationToken ct
        )
        {
            var open = await _context.OtpVerifications
                .Where(row =>
                    row.Email == email
                    && row.Purpose == OtpPurposes.GuestPreferences
                    && row.Channel == OtpVerification.ChannelEmail
                    && !row.IsUsed
                )
                .ToListAsync(ct);
            foreach (var previous in open)
            {
                previous.IsUsed = true;
            }

            var challenge = new OtpVerification
            {
                Email = email,
                OtpCode = string.Empty,
                Channel = OtpVerification.ChannelEmail,
                IsUsed = false,
                ExpiresAt = now.Add(OtpChallengeService.CodeLifetime),
                CreatedAt = now,
                Purpose = OtpPurposes.GuestPreferences,
                SubjectId = subjectId,
                Generation = 0,
                CodeHash = _hasher.HashCode(
                    OtpPurposes.GuestPreferences,
                    subjectId,
                    0,
                    code
                ),
                FailedAttempts = 0,
            };

            _context.OtpVerifications.Add(challenge);
            await _context.SaveChangesAsync(ct);
            return challenge;
        }

        private async Task<int> CountRecentStartsAsync(
            string value,
            string channel,
            DateTime now,
            CancellationToken ct
        )
        {
            var since = now - StartWindow;
            return await _context.OtpVerifications.CountAsync(
                row =>
                    row.Purpose == OtpPurposes.GuestPreferences
                    && row.Channel == channel
                    && row.Email == value
                    && row.CreatedAt >= since,
                ct
            );
        }

        /// <summary>
        /// Entry starts send only when a Master Guest holds the contact, across all restaurants.
        /// </summary>
        private async Task<bool> MatchesMasterGuestAsync(
            string contactType,
            string value,
            CancellationToken ct
        )
        {
            if (contactType == TummlyGuestContactTypes.Email)
            {
                return await _context.MasterGuests.AnyAsync(
                    master => master.NormalizedEmail == value,
                    ct
                );
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            var last9 = digits.Length >= 9 ? digits[^9..] : digits;

            var candidates = await _context.MasterGuests
                .Where(master =>
                    master.NormalizedPhone != null
                    && master.NormalizedPhone.EndsWith(last9)
                )
                .ToListAsync(ct);

            return candidates.Any(master =>
                GuestPhoneMatch.MasterGuestMatchesE164(master, value)
            );
        }

        private string FrontendBaseUrl()
        {
            var baseUrl = _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("Frontend:BaseUrl is not configured.");
            }

            return baseUrl;
        }

        private static bool TryParseEmailLink(
            string v,
            out int challengeId,
            out string code
        )
        {
            challengeId = 0;
            code = string.Empty;

            var separator = v.IndexOf('.');
            if (separator <= 0 || separator == v.Length - 1)
            {
                return false;
            }

            if (
                !int.TryParse(
                    v.AsSpan(0, separator),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out challengeId
                )
            )
            {
                return false;
            }

            code = v[(separator + 1)..];
            return true;
        }

        private static (GuestVerifyOutcome Outcome, ProvenContact? Proven) Invalid() =>
            (GuestVerifyOutcome.Invalid, null);

        private static (GuestVerifyOutcome Outcome, ProvenContact? Proven) Expired() =>
            (GuestVerifyOutcome.Expired, null);
    }
}
