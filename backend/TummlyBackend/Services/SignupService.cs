using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class SignupService : ISignupService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IProvisioningService _provisioningService;
        private readonly ISignupPaySession _signupPaySession;

        public SignupService(
            ApplicationDbContext context,
            IEmailService emailService,
            IProvisioningService provisioningService,
            ISignupPaySession signupPaySession
        )
        {
            _context = context;
            _emailService = emailService;
            _provisioningService = provisioningService;
            _signupPaySession = signupPaySession;
        }

        public async Task<SignupSessionResponse> StartAsync(StartSignupDto dto)
        {
            var email = dto.Email.Trim().ToLower();

            if (!dto.TermsAccepted)
            {
                throw new Exception("Terms must be accepted.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(x =>
                x.Email == email
            );

            if (existingUser != null)
            {
                throw new Exception("Email already in use.");
            }

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email
            );

            var isExistingEmailPending =
                pending != null
                && pending.Status == PendingSignupStatuses.EmailPending;

            if (pending != null)
            {
                if (pending.Status == PendingSignupStatuses.Complete)
                {
                    throw new Exception("Email already in use.");
                }

                // Mid-flow: resume existing session (no duplicate row, no OTP).
                if (
                    pending.Status
                        is PendingSignupStatuses.Verified
                            or PendingSignupStatuses.OnboardingComplete
                            or PendingSignupStatuses.AwaitingPayment
                            or PendingSignupStatuses.Provisioning
                )
                {
                    return ToSessionResponse(pending);
                }

                if (pending.Status == PendingSignupStatuses.Abandoned)
                {
                    pending.Status = PendingSignupStatuses.EmailPending;
                    pending.OtpResendCount = 0;
                    pending.LastOtpSentAt = null;
                    pending.EmailVerifiedAt = null;
                    pending.SessionToken = Guid.NewGuid();
                }

                pending.TermsAccepted = dto.TermsAccepted;
                pending.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                pending = new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    Email = email,
                    Status = PendingSignupStatuses.EmailPending,
                    OtpResendCount = 0,
                    TermsAccepted = dto.TermsAccepted,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                };

                await _context.PendingSignups.AddAsync(pending);
            }

            // Re-POST Start on EmailPending uses the same cooldown / resend cap as Resend.
            if (isExistingEmailPending)
            {
                await EnforceAndSendOtpAsync(pending, countAsResend: true);
            }
            else
            {
                await EnforceAndSendOtpAsync(pending, countAsResend: false);
            }

            return ToSessionResponse(pending);
        }

        public async Task<SignupSessionResponse> VerifyOtpAsync(
            VerifySignupOtpDto dto
        )
        {
            var email = dto.Email.Trim().ToLower();
            var otpCode = dto.OtpCode.Trim();

            var otpRecord = await _context.OtpVerifications.FirstOrDefaultAsync(
                x =>
                    x.Email == email
                    && x.OtpCode == otpCode
                    && x.IsUsed == false
            );

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired OTP.");
            }

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email
                && x.Status == PendingSignupStatuses.EmailPending
            );

            if (pending == null)
            {
                throw new Exception("No pending signup found.");
            }

            otpRecord.IsUsed = true;
            pending.Status = PendingSignupStatuses.Verified;
            pending.EmailVerifiedAt = DateTime.UtcNow;
            pending.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ToSessionResponse(pending);
        }

        public async Task ResendOtpAsync(string email)
        {
            email = email.Trim().ToLower();

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Email == email
                && x.Status == PendingSignupStatuses.EmailPending
            );

            if (pending == null)
            {
                throw new Exception("No pending signup found.");
            }

            await EnforceAndSendOtpAsync(pending, countAsResend: true);
        }

        public async Task<SignupSessionResponse> SaveOnboardingAsync(
            Guid sessionToken,
            SaveSignupOnboardingDto dto
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);

            if (pending.Status == PendingSignupStatuses.Complete)
            {
                return ToSessionResponse(pending);
            }

            if (
                pending.Status
                is not (
                    PendingSignupStatuses.Verified
                    or PendingSignupStatuses.OnboardingComplete
                )
            )
            {
                throw new Exception(
                    "Email must be verified before onboarding."
                );
            }

            ValidateOnboardingPayload(dto);

            pending.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            pending.FullName = dto.FullName.Trim();
            pending.AccountType = "Single";
            pending.OnboardingJson = JsonSerializer.Serialize(
                new SignupOnboardingPayload
                {
                    FullName = pending.FullName,
                    GroupName = dto.GroupName.Trim(),
                    BusinessCategory = dto.BusinessCategory.Trim(),
                    PrimaryPhone = NormalizeOptional(dto.PrimaryPhone),
                    BusinessLink = NormalizeOptional(dto.BusinessLink),
                    Locations = dto.Locations,
                }
            );
            pending.ChosenPlan = BillingSubscriptionPlans.Pilot;
            pending.ChosenCadence = "monthly";
            pending.Status = PendingSignupStatuses.Provisioning;
            pending.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _provisioningService.ProvisionFromPendingAsync(pending.Id);

            await _context.Entry(pending).ReloadAsync();

            return ToSessionResponse(pending);
        }

        public async Task<SignupResumeResponse> GetBySessionAsync(
            Guid sessionToken
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);

            SignupOnboardingPayload? profile = null;
            if (!string.IsNullOrWhiteSpace(pending.OnboardingJson))
            {
                profile = JsonSerializer.Deserialize<SignupOnboardingPayload>(
                    pending.OnboardingJson
                );
            }

            return new SignupResumeResponse
            {
                SessionToken = pending.SessionToken,
                Email = pending.Email,
                Status = pending.Status,
                LastStepHint = ResolveLastStepHint(pending),
                FullName = pending.FullName ?? profile?.FullName,
                AccountType = pending.AccountType,
                GroupName = profile?.GroupName,
                BusinessCategory = profile?.BusinessCategory,
                PrimaryPhone = profile?.PrimaryPhone,
                BusinessLink = profile?.BusinessLink,
                OnboardingJson = pending.OnboardingJson,
                PasswordHash = null,
            };
        }

        public async Task<ChoosePlanResult> ChoosePlanAsync(
            Guid sessionToken,
            string planId,
            string cadence
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);
            var normalizedPlan = (planId ?? string.Empty).Trim();
            var normalizedCadence = string.IsNullOrWhiteSpace(cadence)
                ? "monthly"
                : cadence.Trim().ToLowerInvariant();

            if (pending.Status == PendingSignupStatuses.Complete)
            {
                return new ChoosePlanResult { Mode = "provisioned" };
            }

            if (
                pending.Status
                is not (
                    PendingSignupStatuses.OnboardingComplete
                    or PendingSignupStatuses.Provisioning
                    or PendingSignupStatuses.AwaitingPayment
                )
            )
            {
                throw new Exception(
                    "Onboarding must be complete before choosing a plan."
                );
            }

            if (
                string.Equals(
                    normalizedPlan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                pending.ChosenPlan = BillingSubscriptionPlans.Pilot;
                pending.ChosenCadence = normalizedCadence;
                pending.Status = PendingSignupStatuses.Provisioning;
                pending.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _provisioningService.ProvisionFromPendingAsync(
                    pending.Id
                );

                return new ChoosePlanResult { Mode = "provisioned" };
            }

            var paidPlan = NormalizePaidPlan(normalizedPlan);
            pending.ChosenPlan = paidPlan;
            pending.ChosenCadence = normalizedCadence;
            pending.Status = PendingSignupStatuses.AwaitingPayment;
            pending.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var checkoutUrl = await _signupPaySession.StartAsync(
                pending,
                paidPlan,
                normalizedCadence
            );

            pending.RevolutOrderId = (
                await _context.RevolutOrderIntents
                    .Where(row =>
                        row.PendingSignupId == pending.Id
                        && row.Purpose == RevolutOrderIntentPurposes.SignupPlan
                        && row.IsOpen
                    )
                    .OrderByDescending(row => row.CreatedAtUtc)
                    .Select(row => row.OrderId)
                    .FirstOrDefaultAsync()
            );
            pending.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ChoosePlanResult
            {
                Mode = "checkout",
                CheckoutUrl = checkoutUrl,
            };
        }

        public async Task<SignupProvisioningStatusResponse> GetProvisioningStatusAsync(
            Guid sessionToken
        )
        {
            var pending = await FindBySessionTokenAsync(sessionToken);
            return new SignupProvisioningStatusResponse
            {
                Status = pending.Status,
                Ready = pending.Status == PendingSignupStatuses.Complete,
            };
        }

        private static string NormalizePaidPlan(string planId)
        {
            return planId.Trim().ToLowerInvariant() switch
            {
                "starter" => BillingSubscriptionPlans.Starter,
                "growth" => BillingSubscriptionPlans.Growth,
                "group" => BillingSubscriptionPlans.Group,
                _ => throw new Exception("Invalid plan selection."),
            };
        }

        private async Task<PendingSignup> FindBySessionTokenAsync(
            Guid sessionToken
        )
        {
            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.SessionToken == sessionToken
            );

            if (pending == null)
            {
                throw new Exception("No pending signup found.");
            }

            return pending;
        }

        private static void ValidateOnboardingPayload(
            SaveSignupOnboardingDto dto
        )
        {
            if (dto.Password != dto.ConfirmPassword)
            {
                throw new Exception("Passwords do not match.");
            }

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            {
                throw new Exception(
                    "Password must be at least 8 characters."
                );
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new Exception("Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.GroupName))
            {
                throw new Exception("Group name is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.BusinessCategory))
            {
                throw new Exception("Business category is required.");
            }

            if (dto.Locations == null || dto.Locations.Count != 1)
            {
                throw new Exception("Exactly one location is required.");
            }
        }

        private static string ResolveLastStepHint(PendingSignup pending) =>
            pending.Status switch
            {
                PendingSignupStatuses.EmailPending => "verify",
                PendingSignupStatuses.Verified => string.IsNullOrWhiteSpace(
                    pending.PasswordHash
                )
                    ? "create-password"
                    : "restaurant",
                PendingSignupStatuses.OnboardingComplete => "provisioning",
                PendingSignupStatuses.AwaitingPayment => "payment",
                PendingSignupStatuses.Provisioning => "provisioning",
                PendingSignupStatuses.Complete => "complete",
                PendingSignupStatuses.Abandoned => "restart",
                _ => "restart",
            };

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task EnforceAndSendOtpAsync(
            PendingSignup pending,
            bool countAsResend
        )
        {
            var email = pending.Email;

            if (countAsResend)
            {
                if (pending.LastOtpSentAt == null)
                {
                    pending.LastOtpSentAt = DateTime.UtcNow.AddMinutes(-2);
                }

                if (pending.LastOtpSentAt.Value.AddSeconds(60) > DateTime.UtcNow)
                {
                    throw new Exception("Please wait before resending OTP.");
                }

                if (pending.OtpResendCount >= 5)
                {
                    pending.Status = PendingSignupStatuses.Abandoned;
                    pending.UpdatedAtUtc = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    throw new Exception("OTP resend limit reached.");
                }
            }

            await ReplaceUnusedOtpsAsync(email);

            var otpCode = GenerateOtp.CreateOtp();

            await _context.OtpVerifications.AddAsync(
                new OtpVerification
                {
                    Email = email,
                    OtpCode = otpCode,
                    IsUsed = false,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow,
                }
            );

            if (countAsResend)
            {
                pending.OtpResendCount += 1;
            }

            pending.LastOtpSentAt = DateTime.UtcNow;
            pending.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(email, otpCode);
        }

        private async Task ReplaceUnusedOtpsAsync(string email)
        {
            var oldOtps = await _context
                .OtpVerifications.Where(x =>
                    x.Email == email && x.IsUsed == false
                )
                .ToListAsync();

            if (oldOtps.Count > 0)
            {
                _context.OtpVerifications.RemoveRange(oldOtps);
            }
        }

        private static SignupSessionResponse ToSessionResponse(
            PendingSignup pending
        ) =>
            new()
            {
                SessionToken = pending.SessionToken,
                Email = pending.Email,
                Status = pending.Status,
            };
    }
}
