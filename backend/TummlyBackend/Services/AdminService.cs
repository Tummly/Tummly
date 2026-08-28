    using Microsoft.EntityFrameworkCore;
    using System.Text;
    using TummlyBackend.Data;
    using TummlyBackend.DTOs.Admin;
    using TummlyBackend.Helpers;
    using TummlyBackend.Interfaces;
    using TummlyBackend.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    namespace TummlyBackend.Services
    {
        public class AdminService : IAdminService
        {
            private readonly ApplicationDbContext _context;

            private readonly ITrialReviewTransition _trialReviewTransition;

            private readonly IConfiguration _configuration;

            private readonly ILogger<AdminService> _logger;

            private readonly IAssistantConversationService _assistantConversations;

            private readonly IBillingAccountLifecycle _lifecycle;

            public AdminService(
                ApplicationDbContext context,
                ITrialReviewTransition trialReviewTransition,
                IConfiguration configuration,
                ILogger<AdminService> logger,
                IAssistantConversationService assistantConversations,
                IBillingAccountLifecycle lifecycle
            )
            {
                _context = context;

                _trialReviewTransition = trialReviewTransition;

                _configuration = configuration;

                _logger = logger;

                _assistantConversations = assistantConversations;

                _lifecycle = lifecycle;
            }

            public async Task<OperatorSetupReminderBatchResult>
                ProcessOperatorSetupInvitationRemindersAsync()
            {
                var cutoff =
                    DateTime.UtcNow.AddDays(-TrialReviewConstants.InviteValidityDays);

                var eligibleIds = await _context
                    .TrialRequests
                    .AsNoTracking()
                    .Where(x =>
                        x.IsApproved &&
                        !x.IsAccountCreated &&
                        x.InviteSentAt != null &&
                        x.InviteSentAt <= cutoff &&
                        x.Status != TrialRequestStatus.Declined
                    )
                    .Select(x => x.Id)
                    .ToListAsync();

                var sentCount = 0;
                var failedCount = 0;

                foreach (var trialRequestId in eligibleIds)
                {
                    try
                    {
                        await _trialReviewTransition.ApplyTransitionAsync(
                            trialRequestId,
                            TrialReviewDecision.ResendInvite,
                            new TrialReviewContext(
                                "System",
                                Reason: null,
                                AdminNotes: null
                            )
                        );

                        sentCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogError(
                            ex,
                            "Failed to send Operator Setup invitation reminder for trial request {TrialRequestId}",
                            trialRequestId
                        );
                    }
                }

                if (eligibleIds.Count > 0)
                {
                    _logger.LogInformation(
                        "Operator Setup invitation reminders processed: sent={Sent} failed={Failed} eligible={Eligible}",
                        sentCount,
                        failedCount,
                        eligibleIds.Count
                    );
                }

                return new OperatorSetupReminderBatchResult(
                    Sent: sentCount,
                    Failed: failedCount
                );
            }

            /*
             =========================================
             GET ALL TRIAL REQUESTS
             =========================================
            */

            public async Task<List<AdminTrialRequestDto>>
                GetAllTrialRequestsAsync()
            {
                var trialRequests = await _context
                    .TrialRequests
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

                var trialEmails = trialRequests
                    .Select(request => request.Email.Trim())
                    .Where(email => email.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var locationsByEmail =
                    new Dictionary<string, List<AdminOperatorLocationDto>>(
                        StringComparer.OrdinalIgnoreCase
                    );

                var usersByEmail =
                    new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

                if (trialEmails.Count > 0)
                {
                    var trialEmailSet = new HashSet<string>(
                        trialEmails,
                        StringComparer.OrdinalIgnoreCase
                    );

                    usersByEmail = await _context.Users
                        .AsNoTracking()
                        .Where(user => trialEmailSet.Contains(user.Email))
                        .ToDictionaryAsync(
                            user => user.Email.Trim(),
                            StringComparer.OrdinalIgnoreCase
                        );

                    var locationRows = await (
                        from location in _context.RestaurantLocations.AsNoTracking()
                        join restaurant in _context.Restaurants.AsNoTracking()
                            on location.RestaurantId equals restaurant.Id
                        join user in _context.Users.AsNoTracking()
                            on restaurant.OwnerUserId equals user.Id
                        orderby location.CreatedAt
                        select new
                        {
                            UserEmail = user.Email,
                            Location = new AdminOperatorLocationDto
                            {
                                LocationName = location.LocationName,
                                Address = location.Address,
                                Postcode = location.Postcode,
                                LocationPhone = location.LocationPhone,
                                LocalContact = location.LocalContact,
                            },
                        }
                    ).ToListAsync();

                    foreach (var row in locationRows)
                    {
                        var normalizedUserEmail = row.UserEmail.Trim();

                        if (!trialEmailSet.Contains(normalizedUserEmail))
                        {
                            continue;
                        }

                        var matchingEmail = trialEmails.FirstOrDefault(email =>
                            string.Equals(
                                email,
                                normalizedUserEmail,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );

                        if (matchingEmail == null)
                        {
                            continue;
                        }

                        if (!locationsByEmail.TryGetValue(matchingEmail, out var locations))
                        {
                            locations = new List<AdminOperatorLocationDto>();
                            locationsByEmail[matchingEmail] = locations;
                        }

                        locations.Add(row.Location);
                    }
                }

                return trialRequests
                    .Select(request => MapTrialRequest(
                        request,
                        ResolveOperatorLocations(locationsByEmail, request.Email),
                        ResolveOperatorUser(usersByEmail, request.Email)
                    ))
                    .ToList();
            }

            private static User? ResolveOperatorUser(
                Dictionary<string, User> usersByEmail,
                string email
            )
            {
                var normalizedEmail = email.Trim();

                if (usersByEmail.TryGetValue(normalizedEmail, out var user))
                {
                    return user;
                }

                return usersByEmail
                    .FirstOrDefault(entry =>
                        string.Equals(
                            entry.Key,
                            normalizedEmail,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Value;
            }

            private static List<AdminOperatorLocationDto> ResolveOperatorLocations(
                Dictionary<string, List<AdminOperatorLocationDto>> locationsByEmail,
                string email
            )
            {
                var normalizedEmail = email.Trim();

                if (locationsByEmail.TryGetValue(normalizedEmail, out var locations))
                {
                    return locations;
                }

                return locationsByEmail
                    .FirstOrDefault(entry =>
                        string.Equals(
                            entry.Key,
                            normalizedEmail,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Value ?? new List<AdminOperatorLocationDto>();
            }

            private AdminTrialRequestDto MapTrialRequest(
                TrialRequest request,
                List<AdminOperatorLocationDto> operatorLocations,
                User? operatorUser
            )
            {
                var primaryLocation = operatorLocations.FirstOrDefault();

                var dto = new AdminTrialRequestDto
                {
                    Id = request.Id,
                    BusinessName = request.BusinessName,
                    BusinessCategory = request.BusinessCategory,
                    Locations = request.Locations,
                    BusinessLink = request.BusinessLink,
                    FullName = request.FullName,
                    Email = request.Email,
                    Mobile = request.Mobile,
                    MainLocation = string.IsNullOrWhiteSpace(request.MainLocation)
                        ? string.Empty
                        : request.MainLocation.Trim(),
                    TownCity = string.IsNullOrWhiteSpace(request.TownCity)
                        ? string.Empty
                        : request.TownCity.Trim(),
                    MainLocationPostcode = string.IsNullOrWhiteSpace(request.Postcode)
                        ? string.Empty
                        : request.Postcode.Trim(),
                    Role = request.Role,
                    Goal = request.Goal,
                    IsEmailVerified = request.IsEmailVerified,
                    IsApproved = request.IsApproved,
                    IsAccountCreated = request.IsAccountCreated,
                    AccountType = request.AccountType,
                    Status = request.Status.ToWireString(),
                    CreatedAt = request.CreatedAt,
                    ApprovedAt = request.ApprovedAt,
                    ReviewedAt = request.ReviewedAt,
                    ReviewedBy = request.ReviewedBy,
                    DeclinedAt = request.DeclinedAt,
                    DeclineReason = request.DeclineReason,
                    MoreInfoRequestedAt = request.MoreInfoRequestedAt,
                    MoreInfoMessage = request.MoreInfoMessage,
                    InviteSentAt = request.InviteSentAt,
                    InviteExpiresAt = request.InviteExpiresAt,
                    AccountCreatedAt = request.AccountCreatedAt,
                    PrimaryAddress = string.IsNullOrWhiteSpace(primaryLocation?.Address)
                        ? null
                        : primaryLocation.Address.Trim(),
                    PrimaryPostcode = string.IsNullOrWhiteSpace(primaryLocation?.Postcode)
                        ? null
                        : primaryLocation.Postcode.Trim(),
                    OperatorLocations = operatorLocations,
                };

                if (request.IsAccountCreated && operatorUser != null)
                {
                    var activationSubject =
                        ActivationSubject.FromUser(operatorUser);

                    dto.OperatorUserId = operatorUser.Id;
                    dto.ActivationStatus =
                        ActivationState.IsWithinActivationPeriod(activationSubject)
                            ? "activated"
                            : "not_activated";
                    dto.ActivationStatusDetail =
                        ActivationState.GetStatusDetail(activationSubject)
                        ?? "pending";
                    dto.ActivationExpiresAt = operatorUser.ActivationExpiresAt;

                    var plainCode = ActivationCodeProtectionHelper.Decrypt(
                        operatorUser.ActivationCodeEncrypted,
                        GetActivationProtectionKey()
                    );

                    if (!string.IsNullOrWhiteSpace(plainCode))
                    {
                        dto.ActivationCode = ActivationCodeHelper.FormatCodeForDisplay(
                            ActivationCodeHelper.Normalize(plainCode)
                        );
                    }
                }

                return dto;
            }

            private async Task DeleteRowsByEmailAsync<TEntity>(
                DbSet<TEntity> set,
                string email
            ) where TEntity : class
            {
                var matches = set.Where(
                    row => EF.Property<string>(row, "Email") == email
                );

                if (_context.Database.IsRelational())
                {
                    await matches.ExecuteDeleteAsync();
                    return;
                }

                set.RemoveRange(await matches.ToListAsync());
            }

            private string GetActivationProtectionKey()
            {
                var secret = _configuration["JwtSettings:Secret"];

                if (string.IsNullOrWhiteSpace(secret))
                {
                    throw new InvalidOperationException(
                        "JwtSettings:Secret is required for activation code protection."
                    );
                }

                return secret;
            }

        public bool IsTrialPurgeEnabled()
        {
            return _configuration.GetValue<bool>(
                "Admin:AllowTrialPurge"
            );
        }

        public async Task<bool> PurgeTrialRequestAsync(
            int trialRequestId
        )
        {
            var trialRequest = await _context
                .TrialRequests
                .FirstOrDefaultAsync(x =>
                    x.Id == trialRequestId
                );

            if (trialRequest == null)
            {
                return false;
            }

            var email = trialRequest.Email.Trim();

            await using var transaction =
                _context.Database.IsRelational()
                    ? await _context.Database.BeginTransactionAsync()
                    : null;

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email == email
                    );

                if (user != null)
                {
                    await _assistantConversations.DeleteAllForOwnerAsync(user.Id);
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                }

                await DeleteRowsByEmailAsync(_context.OtpVerifications, email);
                await DeleteRowsByEmailAsync(_context.AccountSetupInvites, email);
                await DeleteRowsByEmailAsync(_context.PendingTrialRequests, email);

                _context.TrialRequests.Remove(trialRequest);
                await _context.SaveChangesAsync();

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return true;
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
        }

        public async Task<AdminTrialRequestDto?> ExtendActivationAsync(
            int userId,
            ExtendActivationDto dto
        )
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                return null;
            }

            if (!ActivationState.IsActivationExpired(ActivationSubject.FromUser(user)))
            {
                throw new ArgumentException(
                    "Only expired accounts can be extended."
                );
            }

            user.ActivationExpiresAt =
                dto.ExpiresAt?.ToUniversalTime()
                ?? ActivationCodeHelper.ComputeDefaultExtensionExpiresAt();

            await _context.SaveChangesAsync();

            var restaurantId = await _context.Restaurants
                .AsNoTracking()
                .Where(row => row.OwnerUserId == user.Id)
                .Select(row => (int?)row.Id)
                .FirstOrDefaultAsync();

            if (restaurantId != null)
            {
                var now = DateTime.UtcNow;
                await _lifecycle.TickAsync(restaurantId.Value, now);
                await _lifecycle.ExtendPilotActivationAsync(
                    restaurantId.Value,
                    user.ActivationExpiresAt.Value,
                    now
                );
            }

            var trialRequest = await _context.TrialRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Email == user.Email && x.IsAccountCreated
                );

            if (trialRequest == null)
            {
                return null;
            }

            var operatorLocations = await (
                from location in _context.RestaurantLocations.AsNoTracking()
                join restaurant in _context.Restaurants.AsNoTracking()
                    on location.RestaurantId equals restaurant.Id
                where restaurant.OwnerUserId == user.Id
                orderby location.CreatedAt
                select new AdminOperatorLocationDto
                {
                    LocationName = location.LocationName,
                    Address = location.Address,
                    Postcode = location.Postcode,
                    LocationPhone = location.LocationPhone,
                    LocalContact = location.LocalContact,
                }
            ).ToListAsync();

            return MapTrialRequest(trialRequest, operatorLocations, user);
        }

        public async Task<(byte[] Content, string FileName, string ContentType)?>
            GetActivationDownloadAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                return null;
            }

            var plainCode = ActivationCodeProtectionHelper.Decrypt(
                user.ActivationCodeEncrypted,
                GetActivationProtectionKey()
            );

            if (string.IsNullOrWhiteSpace(plainCode))
            {
                throw new ArgumentException(
                    "Activation code is not available for this account."
                );
            }

            var trialRequest = await _context.TrialRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == user.Email);

            var displayCode = ActivationCodeHelper.FormatCodeForDisplay(
                ActivationCodeHelper.Normalize(plainCode)
            );
            var businessName = trialRequest?.BusinessName ?? "Tummly operator";
            var svg = BuildActivationCardSvg(
                displayCode,
                user.FullName,
                businessName
            );
            var bytes = Encoding.UTF8.GetBytes(svg);
            var safeBusinessName = string.Concat(
                businessName
                    .Where(character =>
                        char.IsLetterOrDigit(character) || character == '-'
                    )
                    .Take(40)
            );

            if (string.IsNullOrWhiteSpace(safeBusinessName))
            {
                safeBusinessName = "operator";
            }

            return (
                bytes,
                $"tummly-activation-{safeBusinessName.ToLowerInvariant()}.svg",
                "image/svg+xml"
            );
        }

        private static string BuildActivationCardSvg(
            string activationCode,
            string operatorName,
            string businessName
        )
        {
            var escapedCode = System.Security.SecurityElement.Escape(activationCode)
                ?? activationCode;
            var escapedOperator = System.Security.SecurityElement.Escape(operatorName)
                ?? operatorName;
            var escapedBusiness = System.Security.SecurityElement.Escape(businessName)
                ?? businessName;

            return $"""
                <svg xmlns="http://www.w3.org/2000/svg" width="640" height="400" viewBox="0 0 640 400">
                  <rect width="640" height="400" fill="#ffffff" stroke="#d4d4d8" stroke-width="2" rx="16"/>
                  <text x="40" y="70" font-family="Arial, sans-serif" font-size="28" font-weight="700" fill="#111827">Tummly activation</text>
                  <text x="40" y="120" font-family="Arial, sans-serif" font-size="18" fill="#374151">Business: {escapedBusiness}</text>
                  <text x="40" y="155" font-family="Arial, sans-serif" font-size="18" fill="#374151">Operator: {escapedOperator}</text>
                  <text x="40" y="220" font-family="Arial, sans-serif" font-size="16" fill="#6b7280">Activation code</text>
                  <text x="40" y="275" font-family="Courier New, monospace" font-size="42" font-weight="700" fill="#166534" letter-spacing="4">{escapedCode}</text>
                  <text x="40" y="340" font-family="Arial, sans-serif" font-size="14" fill="#6b7280">Enter this code in Sign-in to start your 30-day trial.</text>
                </svg>
                """;
        }
    }
}