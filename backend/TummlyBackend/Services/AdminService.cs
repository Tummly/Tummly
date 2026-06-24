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

            public async Task<List<TrialRequest>>
                GetAllTrialRequestsAsync()
            {
                return await _context
                    .TrialRequests
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();
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

                    trialRequest.DeclineReason =
                        dto.DeclineReason;

                    await _emailService
                        .SendDeclineEmailAsync(
                            trialRequest.Email,
                            trialRequest.FullName
                        );
                }
                else if (
                    dto.Status == "MORE_INFO_REQUESTED"
                )
                {
                    trialRequest.MoreInfoRequestedAt =
                        DateTime.UtcNow;

                    trialRequest.MoreInfoMessage =
                        dto.MoreInfoMessage;

                    await _emailService
                        .SendMoreInfoEmailAsync(
                            trialRequest.Email,
                            trialRequest.FullName
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
                trialRequest.FullName
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
                trialRequest.FullName
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