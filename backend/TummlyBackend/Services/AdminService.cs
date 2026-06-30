    using Microsoft.EntityFrameworkCore;
    using TummlyBackend.Data;
    using TummlyBackend.DTOs.Admin;
    using TummlyBackend.Interfaces;
    using TummlyBackend.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    namespace TummlyBackend.Services
    {
        public class AdminService : IAdminService
        {
            private const int InviteValidityDays = 14;
            private const int MaxAdminFeedbackLength = 2000;

            private readonly ApplicationDbContext _context;

            private readonly IEmailService _emailService;

            private readonly IConfiguration _configuration;

            private readonly ILogger<AdminService> _logger;

            public AdminService(
                ApplicationDbContext context,
                IEmailService emailService,
                IConfiguration configuration,
                ILogger<AdminService> logger
            )
            {
                _context = context;

                _emailService = emailService;

                _configuration = configuration;

                _logger = logger;
            }

            private string GetFrontendBaseUrl()
            {
                var frontendBaseUrl =
                    _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');

                if (string.IsNullOrWhiteSpace(frontendBaseUrl))
                {
                    throw new Exception(
                        "Frontend:BaseUrl is not configured."
                    );
                }

                if (
                    !Uri.TryCreate(
                        frontendBaseUrl,
                        UriKind.Absolute,
                        out var uri
                    ) ||
                    (uri.Scheme != Uri.UriSchemeHttps &&
                        uri.Scheme != Uri.UriSchemeHttp)
                )
                {
                    throw new Exception(
                        "Frontend:BaseUrl must be an absolute http(s) URL."
                    );
                }

                return frontendBaseUrl;
            }

            private static string BuildSetupLink(
                string frontendBaseUrl,
                string accountType,
                string approvalToken
            )
            {
                var route =
                    accountType == "Single"
                        ? "setup-account-single"
                        : "setup-account-multi";

                return
                    $"{frontendBaseUrl}/{route}?token={approvalToken}";
            }

            private static void ValidateAdminFeedback(
                string? feedback,
                string requiredMessage,
                string maxLengthMessage
            )
            {
                if (string.IsNullOrWhiteSpace(feedback))
                {
                    throw new ArgumentException(requiredMessage);
                }

                if (feedback.Length > MaxAdminFeedbackLength)
                {
                    throw new ArgumentException(maxLengthMessage);
                }
            }

            private async Task<string> SendOperatorSetupInvitationAsync(
                TrialRequest trialRequest,
                string statusAfterSend,
                bool isReminder = false
            )
            {
                var newToken = Guid.NewGuid().ToString();
                var now = DateTime.UtcNow;

                trialRequest.ApprovalToken = newToken;
                trialRequest.InviteExpiresAt =
                    now.AddDays(InviteValidityDays);
                trialRequest.InviteSentAt = now;
                trialRequest.Status = statusAfterSend;

                await _context.SaveChangesAsync();

                var setupLink = BuildSetupLink(
                    GetFrontendBaseUrl(),
                    trialRequest.AccountType,
                    newToken
                );

                if (isReminder)
                {
                    await _emailService.SendAccountSetupReminderEmailAsync(
                        trialRequest.Email,
                        trialRequest.FullName,
                        setupLink,
                        trialRequest.InviteExpiresAt!.Value
                    );
                }
                else
                {
                    await _emailService.SendAccountSetupEmailAsync(
                        trialRequest.Email,
                        trialRequest.FullName,
                        setupLink
                    );
                }

                return setupLink;
            }

            public async Task<int>
                ProcessOperatorSetupInvitationRemindersAsync()
            {
                var cutoff =
                    DateTime.UtcNow.AddDays(-InviteValidityDays);

                var eligibleIds = await _context
                    .TrialRequests
                    .AsNoTracking()
                    .Where(x =>
                        x.IsApproved &&
                        !x.IsAccountCreated &&
                        x.InviteSentAt != null &&
                        x.InviteSentAt <= cutoff &&
                        x.Status != "DECLINED"
                    )
                    .Select(x => x.Id)
                    .ToListAsync();

                var sentCount = 0;

                foreach (var trialRequestId in eligibleIds)
                {
                    var trialRequest = await _context
                        .TrialRequests
                        .FirstOrDefaultAsync(x =>
                            x.Id == trialRequestId
                        );

                    if (
                        trialRequest == null ||
                        trialRequest.IsAccountCreated
                    )
                    {
                        continue;
                    }

                    try
                    {
                        await SendOperatorSetupInvitationAsync(
                            trialRequest,
                            "INVITE_SENT",
                            isReminder: true
                        );

                        sentCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to send Operator Setup invitation reminder for trial request {TrialRequestId}",
                            trialRequestId
                        );
                    }
                }

                return sentCount;
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

                if (trialEmails.Count > 0)
                {
                    var trialEmailSet = new HashSet<string>(
                        trialEmails,
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
                        ResolveOperatorLocations(locationsByEmail, request.Email)
                    ))
                    .ToList();
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

            private static AdminTrialRequestDto MapTrialRequest(
                TrialRequest request,
                List<AdminOperatorLocationDto> operatorLocations
            )
            {
                var primaryLocation = operatorLocations.FirstOrDefault();

                return new AdminTrialRequestDto
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
                    Status = request.Status,
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
            }

            /*
             =========================================
             UPDATE TRIAL STATUS
             =========================================
            */

            public async Task<bool>
                UpdateTrialStatusAsync(
                    UpdateTrialStatusDto dto
                )
            {
                var trialRequest = await _context
                    .TrialRequests
                    .FirstOrDefaultAsync(x =>
                        x.Id == dto.TrialRequestId
                    );

                if (trialRequest == null)
                {
                    return false;
                }

                if (dto.Status == "DECLINED")
                {
                    ValidateAdminFeedback(
                        dto.DeclineReason,
                        "Decline reason is required.",
                        "Decline reason must be 2000 characters or fewer."
                    );
                }
                else if (dto.Status == "MORE_INFO_REQUESTED")
                {
                    ValidateAdminFeedback(
                        dto.MoreInfoMessage,
                        "More info message is required.",
                        "More info message must be 2000 characters or fewer."
                    );
                }

                /*
                 =========================================
                 UPDATE STATUS
                 =========================================
                */

                trialRequest.Status =
                    dto.Status;


                trialRequest.ReviewedAt =
                     DateTime.UtcNow;

                trialRequest.ReviewedBy =
                    "Admin";

                trialRequest.AdminNotes =
                    dto.AdminNotes;
                /*
                 =========================================
                 TRIGGER CONDITIONAL STATUS EMAILS
                 =========================================
                */

                if (dto.Status == "DECLINED")
                {
                    trialRequest.DeclinedAt =
                        DateTime.UtcNow;

                    var declineReason = dto.DeclineReason!.Trim();
                    trialRequest.DeclineReason = declineReason;

                    await _emailService
                        .SendDeclineEmailAsync(
                            trialRequest.Email,
                            trialRequest.FullName,
                            declineReason
                        );
                }
                else if (
                    dto.Status == "MORE_INFO_REQUESTED"
                )
                {
                    trialRequest.MoreInfoRequestedAt =
                        DateTime.UtcNow;

                    var moreInfoMessage = dto.MoreInfoMessage!.Trim();
                    trialRequest.MoreInfoMessage = moreInfoMessage;

                    await _emailService
                        .SendMoreInfoEmailAsync(
                            trialRequest.Email,
                            trialRequest.FullName,
                            moreInfoMessage
                        );
                }

                /*
                 =========================================
                 SAVE DATABASE
                 =========================================
                */

                await _context.SaveChangesAsync();

                return true;
            }

            /*
             =========================================
             APPROVE TRIAL REQUEST
             =========================================
            */

            public async Task<object>
                ApproveTrialRequestAsync(
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
                    throw new Exception(
                        "Trial request not found."
                    );
                }

                /*
                 =========================================
                 GENERATE APPROVAL TOKEN
                 =========================================
                */

                /*
                 =========================================
                 UPDATE REQUEST
                 =========================================
                */

                trialRequest.IsApproved = true;

                trialRequest.ApprovedAt =
                    DateTime.UtcNow;

                trialRequest.ReviewedAt =
                    DateTime.UtcNow;

                trialRequest.ReviewedBy =
                    "Admin";

                trialRequest.AccountType =
                    trialRequest.Locations == "1"
                        ? "Single"
                        : "Multi";

                var setupLink =
                    await SendOperatorSetupInvitationAsync(
                        trialRequest,
                        "APPROVED"
                    );

                return new
                {
                    success = true,

                    message =
                        "Trial request approved successfully.",

                    inviteToken =
                        trialRequest.ApprovalToken,

                    accountType =
                        trialRequest.AccountType,

                    expiresAt =
                        trialRequest.InviteExpiresAt,

                    setupLink =
                        setupLink
                };
            }

            /*
             =========================================
             RESEND INVITE
             =========================================
            */

            public async Task<object>
                ResendInviteAsync(
                    int trialRequestId
                )
            {
                /*
                 =========================================
                 FIND REQUEST
                 =========================================
                */

                var trialRequest =
                    await _context
                        .TrialRequests
                        .FirstOrDefaultAsync(x =>
                            x.Id == trialRequestId
                        );

                /*
                 =========================================
                 NOT FOUND
                 =========================================
                */

                if (trialRequest == null)
                {
                    throw new Exception(
                        "Trial request not found."
                    );
                }

                /*
                 =========================================
                 ACCOUNT ALREADY CREATED
                 =========================================
                */

                if (
                    trialRequest.IsAccountCreated
                )
                {
                    throw new Exception(
                        "Account already created."
                    );
                }

                var setupLink =
                    await SendOperatorSetupInvitationAsync(
                        trialRequest,
                        "INVITE_SENT",
                        isReminder: true
                    );

                return new
                {
                    success = true,

                    message =
                        "Invite resent successfully.",

                    setupLink
                };

            }
        public async Task<object> DeclineRequestAsync(int trialRequestId)
        {
            var trialRequest = await _context.TrialRequests
                .FirstOrDefaultAsync(x => x.Id == trialRequestId);

            if (trialRequest == null)
                throw new Exception("Trial request not found.");

            trialRequest.Status = "DECLINED";
            trialRequest.DeclinedAt = DateTime.UtcNow;

            await _emailService.SendDeclineEmailAsync(
                trialRequest.Email,
                trialRequest.FullName,
                string.Empty
            );

            await _context.SaveChangesAsync();

            return new
            {
                success = true,
                message = "Request declined successfully"
            };
        }

        public async Task<object> RequestMoreInfoAsync(int trialRequestId)
        {
            var trialRequest = await _context.TrialRequests
                .FirstOrDefaultAsync(x => x.Id == trialRequestId);

            if (trialRequest == null)
                throw new Exception("Trial request not found.");

            trialRequest.Status = "MORE_INFO_REQUESTED";
            trialRequest.MoreInfoRequestedAt = DateTime.UtcNow;

            await _emailService.SendMoreInfoEmailAsync(
                trialRequest.Email,
                trialRequest.FullName,
                string.Empty
            );

            await _context.SaveChangesAsync();

            return new
            {
                success = true,
                message = "More info email sent successfully"
            };
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
                await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email == email
                    );

                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                }

                await _context.OtpVerifications
                    .Where(o => o.Email == email)
                    .ExecuteDeleteAsync();

                await _context.AccountSetupInvites
                    .Where(i => i.Email == email)
                    .ExecuteDeleteAsync();

                await _context.PendingTrialRequests
                    .Where(p => p.Email == email)
                    .ExecuteDeleteAsync();

                _context.TrialRequests.Remove(trialRequest);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}