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

                var frontendBaseUrl =
                    _configuration["Frontend:BaseUrl"];

            string setupLink;

            if (trialRequest.AccountType == "Single")
            {
                setupLink =
                    $"{frontendBaseUrl}/setup-account-single?token={approvalToken}";
            }
            else
            {
                setupLink =
                    $"{frontendBaseUrl}/setup-account-multi?token={approvalToken}";
            }

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

                var frontendBaseUrl =
                    _configuration["Frontend:BaseUrl"];

            string setupLink;

            if (trialRequest.AccountType == "Single")
            {
                setupLink =
                    $"{frontendBaseUrl}/setup-account-single?token={newToken}";
            }
            else
            {
                setupLink =
                    $"{frontendBaseUrl}/setup-account-multi?token={newToken}";
            }

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
    }

    }