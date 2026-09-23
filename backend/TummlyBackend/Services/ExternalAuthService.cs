using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ExternalAuthService : IExternalAuthService
    {
        private static readonly TimeSpan TicketTtl = TimeSpan.FromMinutes(10);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly ApplicationDbContext _context;
        private readonly IExternalOAuthProviderClient _providerClient;
        private readonly IAuthService _authService;
        private readonly ILogger<ExternalAuthService> _logger;
        private readonly string _frontendBaseUrl;

        public ExternalAuthService(
            ApplicationDbContext context,
            IExternalOAuthProviderClient providerClient,
            IAuthService authService,
            IConfiguration configuration,
            ILogger<ExternalAuthService> logger
        )
        {
            _context = context;
            _providerClient = providerClient;
            _authService = authService;
            _logger = logger;

            var baseUrl =
                configuration["Frontend:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured."
                );
            _frontendBaseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<string> BuildStartRedirectAsync(
            string provider,
            string? returnPath,
            CancellationToken cancellationToken = default
        )
        {
            var canonical = CanonicalProvider(provider);
            var path = NormalizeReturnPath(returnPath);
            var token = CreateTicketToken();

            var ticket = new ExternalAuthTicket
            {
                Id = Guid.NewGuid(),
                Token = token,
                Purpose = ExternalAuthTicketPurposes.OAuthState,
                PayloadJson = JsonSerializer.Serialize(
                    new OAuthStatePayload
                    {
                        Provider = canonical,
                        ReturnPath = path,
                        Nonce = Guid.NewGuid().ToString("N"),
                    }
                ),
                ExpiresAtUtc = DateTime.UtcNow.Add(TicketTtl),
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _context.ExternalAuthTickets.AddAsync(ticket, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return _providerClient.BuildAuthorizationUrl(canonical, token);
        }

        public async Task<string> HandleCallbackAsync(
            string provider,
            string? code,
            string? state,
            string? error,
            CancellationToken cancellationToken = default
        )
        {
            var canonical = CanonicalProvider(provider);
            var returnPath = "/login";

            try
            {
                if (
                    string.Equals(
                        error,
                        "access_denied",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    returnPath = await TryConsumeStateReturnPathAsync(
                        state,
                        canonical,
                        cancellationToken
                    );
                    return FrontendErrorUrl(returnPath, "cancelled");
                }

                var statePayload = await ConsumeTicketAsync<OAuthStatePayload>(
                    state,
                    ExternalAuthTicketPurposes.OAuthState,
                    cancellationToken
                );
                returnPath = NormalizeReturnPath(statePayload.ReturnPath);

                if (
                    !string.Equals(
                        statePayload.Provider,
                        canonical,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return FrontendErrorUrl(returnPath, "failed");
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    return FrontendErrorUrl(returnPath, "failed");
                }

                ExternalOAuthProfile profile;
                try
                {
                    profile = await _providerClient.ExchangeCodeAsync(
                        canonical,
                        code,
                        cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "External auth provider token exchange failed for {Provider}",
                        canonical
                    );
                    return FrontendErrorUrl(returnPath, "provider_failed");
                }

                var email = profile.Email.Trim().ToLowerInvariant();
                var subject = profile.Subject.Trim();

                var linked = await _context.UserExternalLogins
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.Provider == canonical
                            && x.ProviderSubject == subject,
                        cancellationToken
                    );

                if (linked != null)
                {
                    var signInToken = await CreateExchangeTicketAsync(
                        ExternalAuthTicketPurposes.SignInExchange,
                        JsonSerializer.Serialize(
                            new SignInExchangePayload { UserId = linked.UserId }
                        ),
                        cancellationToken
                    );
                    // Query token survives HTTP 302 Location; SPA strips it after read.
                    return $"{_frontendBaseUrl}/login/oauth/complete?token={Uri.EscapeDataString(signInToken)}";
                }

                if (await UserEmailExistsAsync(email, cancellationToken))
                {
                    return FrontendErrorUrl(returnPath, "account_exists");
                }

                if (await StaffEmailExistsAsync(email, cancellationToken))
                {
                    return FrontendErrorUrl(returnPath, "staff_password_only");
                }

                if (await PendingBlocksSocialSignupAsync(email, cancellationToken))
                {
                    return FrontendErrorUrl(returnPath, "account_exists");
                }

                var signUpToken = await CreateExchangeTicketAsync(
                    ExternalAuthTicketPurposes.SignUpExchange,
                    JsonSerializer.Serialize(
                        new SignUpExchangePayload
                        {
                            Email = email,
                            Provider = canonical,
                            ProviderSubject = subject,
                            FullName = string.IsNullOrWhiteSpace(profile.FullName)
                                ? null
                                : profile.FullName.Trim(),
                        }
                    ),
                    cancellationToken
                );

                // Query token survives HTTP 302 Location; SPA strips it after read.
                return $"{_frontendBaseUrl}/signup/oauth/terms?token={Uri.EscapeDataString(signUpToken)}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "External auth callback failed for provider {Provider}",
                    canonical
                );
                return FrontendErrorUrl(returnPath, "failed");
            }
        }

        public async Task<object> ExchangeSignInAsync(
            ExternalAuthExchangeDto dto,
            CancellationToken cancellationToken = default
        )
        {
            var payload = await ConsumeTicketAsync<SignInExchangePayload>(
                dto.Token,
                ExternalAuthTicketPurposes.SignInExchange,
                cancellationToken
            );

            if (payload.UserId <= 0)
            {
                throw new Exception("Invalid exchange token.");
            }

            return await _authService.CompleteExternalOperatorSignInAsync(
                payload.UserId,
                dto.RememberDevice,
                dto.DeviceToken
            );
        }

        public async Task<SignupSessionResponse> AcceptTermsAsync(
            ExternalAuthAcceptTermsDto dto,
            CancellationToken cancellationToken = default
        )
        {
            if (!dto.TermsAccepted)
            {
                throw new Exception("Terms must be accepted.");
            }

            // Peek first so conflict / late-stage failures do not burn the ticket.
            var payload = await PeekTicketAsync<SignUpExchangePayload>(
                dto.Token,
                ExternalAuthTicketPurposes.SignUpExchange,
                cancellationToken
            );

            var email = payload.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new Exception("Invalid exchange token.");
            }

            if (await UserEmailExistsAsync(email, cancellationToken))
            {
                throw new Exception("Email already in use.");
            }

            if (await StaffEmailExistsAsync(email, cancellationToken))
            {
                throw new Exception(
                    "Staff accounts must use email and password Sign-in."
                );
            }

            var pending = await _context.PendingSignups.FirstOrDefaultAsync(
                x => x.Email.ToLower() == email,
                cancellationToken
            );

            if (pending != null && !CanResumePendingForSocialSignup(pending.Status))
            {
                throw new Exception("Email already in use.");
            }

            await ConsumeTicketAsync<SignUpExchangePayload>(
                dto.Token,
                ExternalAuthTicketPurposes.SignUpExchange,
                cancellationToken
            );

            var now = DateTime.UtcNow;

            if (pending == null)
            {
                pending = new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    Email = email,
                    CreatedAtUtc = now,
                };
                await _context.PendingSignups.AddAsync(pending, cancellationToken);
            }

            pending.AuthProvider = payload.Provider;
            pending.ProviderSubject = payload.ProviderSubject;
            pending.TermsAccepted = true;
            pending.Status = PendingSignupStatuses.Verified;
            pending.EmailVerifiedAt = now;
            pending.PasswordHash = null;
            pending.UpdatedAtUtc = now;

            if (
                string.IsNullOrWhiteSpace(pending.FullName)
                && !string.IsNullOrWhiteSpace(payload.FullName)
            )
            {
                pending.FullName = payload.FullName.Trim();
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new SignupSessionResponse
            {
                SessionToken = pending.SessionToken,
                Email = pending.Email,
                Status = pending.Status,
            };
        }

        private async Task<bool> UserEmailExistsAsync(
            string emailLower,
            CancellationToken cancellationToken
        ) =>
            await _context.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.Email.ToLower() == emailLower,
                    cancellationToken
                );

        private async Task<bool> StaffEmailExistsAsync(
            string emailLower,
            CancellationToken cancellationToken
        ) =>
            await _context.Admins
                .AsNoTracking()
                .AnyAsync(
                    x => x.Email.ToLower() == emailLower,
                    cancellationToken
                );

        /// <summary>
        /// Complete or late-stage pending rows must not be reset by social Terms.
        /// </summary>
        private async Task<bool> PendingBlocksSocialSignupAsync(
            string emailLower,
            CancellationToken cancellationToken
        )
        {
            var pending = await _context.PendingSignups
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Email.ToLower() == emailLower,
                    cancellationToken
                );

            if (pending == null)
            {
                return false;
            }

            return !CanResumePendingForSocialSignup(pending.Status);
        }

        private static bool CanResumePendingForSocialSignup(string status) =>
            status
                is PendingSignupStatuses.EmailPending
                    or PendingSignupStatuses.Verified
                    or PendingSignupStatuses.Abandoned;

        private async Task<string> TryConsumeStateReturnPathAsync(
            string? state,
            string canonicalProvider,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var payload = await ConsumeTicketAsync<OAuthStatePayload>(
                    state,
                    ExternalAuthTicketPurposes.OAuthState,
                    cancellationToken
                );
                if (
                    !string.Equals(
                        payload.Provider,
                        canonicalProvider,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return "/login";
                }

                return NormalizeReturnPath(payload.ReturnPath);
            }
            catch
            {
                return "/login";
            }
        }

        private async Task<T> PeekTicketAsync<T>(
            string? token,
            string purpose,
            CancellationToken cancellationToken
        )
            where T : class
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("Invalid or expired token.");
            }

            var ticket = await _context.ExternalAuthTickets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Token == token,
                    cancellationToken
                );

            if (
                ticket == null
                || ticket.Purpose != purpose
                || ticket.ConsumedAtUtc != null
                || ticket.ExpiresAtUtc < DateTime.UtcNow
            )
            {
                throw new Exception("Invalid or expired token.");
            }

            var payload = JsonSerializer.Deserialize<T>(
                ticket.PayloadJson,
                JsonOptions
            );
            if (payload == null)
            {
                throw new Exception("Invalid or expired token.");
            }

            return payload;
        }

        private async Task<T> ConsumeTicketAsync<T>(
            string? token,
            string purpose,
            CancellationToken cancellationToken
        )
            where T : class
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("Invalid or expired token.");
            }

            var ticket = await _context.ExternalAuthTickets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Token == token,
                    cancellationToken
                );

            if (
                ticket == null
                || ticket.Purpose != purpose
                || ticket.ConsumedAtUtc != null
                || ticket.ExpiresAtUtc < DateTime.UtcNow
            )
            {
                throw new Exception("Invalid or expired token.");
            }

            var now = DateTime.UtcNow;
            var payloadJson = ticket.PayloadJson;
            var consumed = false;

            // Atomic one-time consume on relational providers:
            // UPDATE ... SET ConsumedAtUtc = @now WHERE Id = @id AND ConsumedAtUtc IS NULL
            try
            {
                var affected = await _context.ExternalAuthTickets
                    .Where(x => x.Id == ticket.Id && x.ConsumedAtUtc == null)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(x => x.ConsumedAtUtc, now),
                        cancellationToken
                    );

                if (affected == 1)
                {
                    consumed = true;
                }
            }
            catch (InvalidOperationException)
            {
                // Provider rejected ExecuteUpdate (e.g. EF InMemory).
            }

            if (!consumed)
            {
                // Fallback: tracked SaveChanges with ConsumedAtUtc == null check.
                // On SQL, affected==0 means a parallel caller already won — reload
                // sees ConsumedAtUtc set and we throw. On InMemory this path is
                // the only workable consume (no real concurrency control).
                _context.ChangeTracker.Clear();
                var tracked = await _context.ExternalAuthTickets
                    .FirstOrDefaultAsync(
                        x => x.Id == ticket.Id,
                        cancellationToken
                    );

                if (
                    tracked == null
                    || tracked.Purpose != purpose
                    || tracked.ConsumedAtUtc != null
                    || tracked.ExpiresAtUtc < DateTime.UtcNow
                )
                {
                    throw new Exception("Invalid or expired token.");
                }

                tracked.ConsumedAtUtc = now;
                await _context.SaveChangesAsync(cancellationToken);
                payloadJson = tracked.PayloadJson;
            }

            var payload = JsonSerializer.Deserialize<T>(
                payloadJson,
                JsonOptions
            );
            if (payload == null)
            {
                throw new Exception("Invalid or expired token.");
            }

            return payload;
        }

        private async Task<string> CreateExchangeTicketAsync(
            string purpose,
            string payloadJson,
            CancellationToken cancellationToken
        )
        {
            var token = CreateTicketToken();
            await _context.ExternalAuthTickets.AddAsync(
                new ExternalAuthTicket
                {
                    Id = Guid.NewGuid(),
                    Token = token,
                    Purpose = purpose,
                    PayloadJson = payloadJson,
                    ExpiresAtUtc = DateTime.UtcNow.Add(TicketTtl),
                    CreatedAtUtc = DateTime.UtcNow,
                },
                cancellationToken
            );
            await _context.SaveChangesAsync(cancellationToken);
            return token;
        }

        private string FrontendErrorUrl(string returnPath, string errorCode)
        {
            var page = IsSignupReturnPath(returnPath) ? "/signup" : "/login";
            return $"{_frontendBaseUrl}{page}?oauthError={Uri.EscapeDataString(errorCode)}";
        }

        private static string CanonicalProvider(string provider)
        {
            if (
                string.Equals(
                    provider,
                    ExternalAuthProviders.Google,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return ExternalAuthProviders.Google;
            }

            if (
                string.Equals(
                    provider,
                    ExternalAuthProviders.Microsoft,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return ExternalAuthProviders.Microsoft;
            }

            throw new InvalidOperationException(
                $"Unsupported external auth provider '{provider}'."
            );
        }

        private static string NormalizeReturnPath(string? returnPath)
        {
            if (string.IsNullOrWhiteSpace(returnPath))
            {
                return "/login";
            }

            var trimmed = returnPath.Trim();
            if (!trimmed.StartsWith('/'))
            {
                trimmed = "/" + trimmed;
            }

            return trimmed;
        }

        private static bool IsSignupReturnPath(string returnPath) =>
            returnPath.StartsWith("/signup", StringComparison.OrdinalIgnoreCase);

        private static string CreateTicketToken() =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        private sealed class OAuthStatePayload
        {
            public string Provider { get; set; } = "";
            public string ReturnPath { get; set; } = "/login";
            public string Nonce { get; set; } = "";
        }

        private sealed class SignInExchangePayload
        {
            public int UserId { get; set; }
        }

        private sealed class SignUpExchangePayload
        {
            public string Email { get; set; } = "";
            public string Provider { get; set; } = "";
            public string ProviderSubject { get; set; } = "";
            public string? FullName { get; set; }
        }
    }
}
