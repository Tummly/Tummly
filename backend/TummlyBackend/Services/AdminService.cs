    using Microsoft.EntityFrameworkCore;
    using TummlyBackend.Data;
    using TummlyBackend.DTOs.Admin;
    using TummlyBackend.Interfaces;
    using TummlyBackend.Models;
    using Microsoft.Extensions.Configuration;

    namespace TummlyBackend.Services
    {
        public class AdminService : IAdminService
        {
            private readonly ApplicationDbContext _context;

            private readonly IEmailService _emailService;

            private readonly IConfiguration _configuration;

            public AdminService(
                ApplicationDbContext context,
                IEmailService emailService,
                IConfiguration configuration
            )
            {
                _context = context;

                _emailService = emailService;

                _configuration = configuration;
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

                var approvalToken =
                    Guid.NewGuid().ToString();

                /*
                 =========================================
                 UPDATE REQUEST
                 =========================================
                */

                trialRequest.IsApproved = true;

                trialRequest.Status =
                    "APPROVED";

                trialRequest.ApprovedAt =
                    DateTime.UtcNow;

                trialRequest.ApprovalToken =
                    approvalToken;

                trialRequest.InviteSentAt =
                    DateTime.UtcNow;

                trialRequest.ReviewedAt =
                    DateTime.UtcNow;

                trialRequest.ReviewedBy =
                    "Admin";

                /*
                 =========================================
                 ACCOUNT TYPE AUTO MAPPING
                 =========================================
                */

                trialRequest.AccountType =
                    trialRequest.Locations == "1"
                        ? "Single"
                        : "Multi";

                /*
                 =========================================
                 14 DAY EXPIRY
                 =========================================
                */

                trialRequest.InviteExpiresAt =
                    DateTime.UtcNow.AddDays(14);

                /*
                 =========================================
                 SAVE DATABASE
                 =========================================
                */

                await _context.SaveChangesAsync();

                /*
                 =========================================
                 FRONTEND SETUP LINK
                 =========================================
                */

                var frontendBaseUrl = GetFrontendBaseUrl();

                var setupLink = BuildSetupLink(
                    frontendBaseUrl,
                    trialRequest.AccountType,
                    approvalToken
                );

            /*
             =========================================
             SEND ACCOUNT SETUP EMAIL
             =========================================
            */

            await _emailService
                    .SendAccountSetupEmailAsync(
                        trialRequest.Email,
                        trialRequest.FullName,
                        setupLink
                    );

                /*
                 =========================================
                 RESPONSE
                 =========================================
                */

                return new
                {
                    success = true,

                    message =
                        "Trial request approved successfully.",

                    inviteToken =
                        approvalToken,

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

                /*
                 =========================================
                 GENERATE NEW TOKEN
                 =========================================
                */

                var newToken =
                    Guid.NewGuid().ToString();

                /*
                 =========================================
                 UPDATE TOKEN
                 =========================================
                */

                trialRequest.ApprovalToken =
                    newToken;

                /*
                 =========================================
                 NEW 14 DAY EXPIRY
                 =========================================
                */

                trialRequest.InviteExpiresAt =
                    DateTime.UtcNow.AddDays(14);

                /*
                 =========================================
                 UPDATE STATUS
                 =========================================
                */

                trialRequest.Status =
         "INVITE_SENT";

                trialRequest.InviteSentAt =
                    DateTime.UtcNow;

                /*
                 =========================================
                 SAVE DATABASE
                 =========================================
                */

                await _context.SaveChangesAsync();

                /*
                 =========================================
                 FRONTEND URL
                 =========================================
                */

                var frontendBaseUrl = GetFrontendBaseUrl();

                var setupLink = BuildSetupLink(
                    frontendBaseUrl,
                    trialRequest.AccountType,
                    newToken
                );

            /*
             =========================================
             SEND EMAIL
             =========================================
            */

            await _emailService
                    .SendAccountSetupEmailAsync(
                        trialRequest.Email,
                        trialRequest.FullName,
                        setupLink
                    );

                /*
                 =========================================
                 RESPONSE
                 =========================================
                */
                    
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