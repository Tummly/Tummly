using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Scan;
using TummlyBackend.DTOs.SmartGuestLink;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/scan")]
    public class ScanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly IGuestUpsertService _guestUpsert;
        private readonly ILocationGuestActivityRecorder _recorder;
        private readonly IMemoryCache _cache;
        private readonly IFeedbackClassificationWork _classificationWork;
        private readonly ISpeechToTextProvider _speechToText;
        private readonly IOfferIssueService _offerIssues;
        private readonly IGuestFormPermissionApplyService _guestFormPermissions;
        private readonly IGuestFormSubmitLock _submitLock;
        private readonly IRecaptchaVerifier _recaptcha;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ScanController> _logger;
        private static readonly TimeSpan DuplicateSubmitWindow = TimeSpan.FromMinutes(10);
        /// <summary>
        /// Concurrent loser may re-show before the winner issues the Claim.
        /// Only poll when the duplicate Feedback is this fresh.
        /// </summary>
        private static readonly TimeSpan ConcurrentClaimWaitFreshWindow =
            TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ConcurrentClaimPollBudget =
            TimeSpan.FromSeconds(2);
        private static readonly TimeSpan ConcurrentClaimPollStep =
            TimeSpan.FromMilliseconds(150);

        public ScanController(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink,
            IGuestUpsertService guestUpsert,
            ILocationGuestActivityRecorder recorder,
            IMemoryCache cache,
            IFeedbackClassificationWork classificationWork,
            ISpeechToTextProvider speechToText,
            IOfferIssueService offerIssues,
            IGuestFormPermissionApplyService guestFormPermissions,
            IGuestFormSubmitLock submitLock,
            IRecaptchaVerifier recaptcha,
            IConfiguration configuration,
            ILogger<ScanController> logger
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
            _guestUpsert = guestUpsert;
            _recorder = recorder;
            _cache = cache;
            _classificationWork = classificationWork;
            _speechToText = speechToText;
            _offerIssues = offerIssues;
            _guestFormPermissions = guestFormPermissions;
            _submitLock = submitLock;
            _recaptcha = recaptcha;
            _configuration = configuration;
            _logger = logger;
        }

        /*
         =========================================
         GET LOCATION METADATA BY TOKEN
         =========================================
        */

        [HttpGet("{token}")]
        public async Task<IActionResult> GetLocationByToken(
            string token
        )
        {
            var normalizedToken = token?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return NotFound(new
                {
                    success = false,
                    message = "Invalid link."
                });
            }

            var resolution = await _smartGuestLink.ResolveForGuestAsync(
                normalizedToken
            );

            switch (resolution)
            {
                case GuestQrResolveResult.NotFound:
                    return NotFound(new
                    {
                        success = false,
                        message = "Link not found."
                    });

                case GuestQrResolveResult.Dormant dormant:
                    return Ok(new
                    {
                        status = "dormant",
                        restaurantName = dormant.RestaurantName,
                        brandLogoPublicUrl = dormant.BrandLogoPublicUrl
                    });

                case GuestQrResolveResult.Live live:
                {
                    var location = live.Location;

                    // First-party scan + form-open events for Capture / Reports KPIs
                    // (QR, shared Smart Guest Link, and operator Preview form).
                    var openedAt = DateTime.UtcNow;
                    var isDemo =
                        IsMaveroDemoToken(normalizedToken)
                        || await DemoRestaurant.IsDemoLocationAsync(
                            _context,
                            DemoOptions(),
                            location.LocationId,
                            HttpContext.RequestAborted
                        );
                    // Product-demo QR: visits must not inflate shared KPIs.
                    if (!isDemo)
                    {
                        _context.QrScanEvents.Add(
                            new QrScanEvent
                            {
                                RestaurantLocationId = location.LocationId,
                                QrCodeId = location.QrCodeId,
                                CreatedAt = openedAt
                            }
                        );
                        _context.QrFormOpenEvents.Add(
                            new QrFormOpenEvent
                            {
                                RestaurantLocationId = location.LocationId,
                                QrCodeId = location.QrCodeId,
                                CreatedAt = openedAt
                            }
                        );
                        await _context.SaveChangesAsync();
                    }

                    var restaurant = await LoadRestaurantForLocationAsync(
                        location.LocationId
                    );
                    var guestFormConsent = BuildGuestFormConsentDto(restaurant);

                    return Ok(new
                    {
                        success = true,
                        restaurantName = location.RestaurantName,
                        locationName = location.LocationName,
                        address = location.Address,
                        brandLogoPublicUrl = location.BrandLogoPublicUrl,
                        guestFormConsent,
                    });
                }

                default:
                    throw new InvalidOperationException(
                        $"Unexpected guest QR resolve result: {resolution.GetType().Name}."
                    );
            }
        }

        /*
         =========================================
         SUBMIT FEEDBACK BY TOKEN
         =========================================
        */

        [HttpPost("{token}/feedback")]
        public async Task<IActionResult> SubmitFeedback(
            string token,
            [FromBody] FeedbackSubmissionDto dto,
            CancellationToken cancellationToken
        )
        {
            var normalizedToken = token?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return NotFound(new
                {
                    success = false,
                    message = "Invalid link."
                });
            }

            var resolution = await _smartGuestLink
                .ResolveLocationForWriteAsync(normalizedToken);

            if (resolution == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Link not found."
                });
            }

            var location = resolution.Location;

            /*
             =========================================
             FIELD VALIDATION
             =========================================
            */

            if (string.IsNullOrWhiteSpace(dto?.GuestName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "First name is required."
                });
            }

            if (dto!.GuestName.Length > 150)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "First name must be 150 characters or fewer."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.GuestContact))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Contact is required."
                });
            }

            if (dto.GuestContact.Length > 100)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Contact must be 100 characters or fewer."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Comment))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Message is required."
                });
            }

            if (dto.Comment.Length > 1000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Message must be 1000 characters or fewer."
                });
            }

            if (!GuestFormContactValidate.TryResolve(dto.GuestContact, out var contactType))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Enter a valid email or UK mobile number.",
                });
            }

            if (await IsDemoGuestFormAsync(normalizedToken, location.RestaurantId))
            {
                // Product-demo QR: never store visitor Feedback, identity or
                // consent, and never issue Offers or send messages (ENG07).
                return Ok(new
                {
                    success = true,
                    demo = true,
                    message = DemoRestaurant.NothingSavedMessage,
                });
            }

            /*
             =========================================
             reCAPTCHA v3 (when Recaptcha:SecretKey is set)
             =========================================
            */

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var captcha = await _recaptcha.VerifyGuestFeedbackAsync(
                dto.RecaptchaToken,
                clientIp
            );
            if (
                captcha.Status
                is RecaptchaVerifyStatus.MissingToken
                    or RecaptchaVerifyStatus.Failed
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message = captcha.Message
                        ?? "Security check failed. Please try again.",
                });
            }

            if (
                !GuestContactKey.TryCreate(
                    dto.GuestContact,
                    contactType,
                    out var contactKey
                )
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Enter a valid email or UK mobile number.",
                });
            }

            var guestName = dto.GuestName.Trim();
            var guestContact = dto.GuestContact.Trim();
            var comment = dto.Comment.Trim();

            /*
             =========================================
             PER-CONTACT LOCK (serialise concurrent submits)
             =========================================
            */

            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            Feedback? duplicate = null;
            Feedback? feedback = null;
            try
            {
                if (
                    !await _submitLock.TryAcquireAsync(
                        _context,
                        location.RestaurantId,
                        contactKey,
                        cancellationToken
                    )
                )
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    return StatusCode(
                        StatusCodes.Status503ServiceUnavailable,
                        new
                        {
                            success = false,
                            message = "Please try again.",
                        }
                    );
                }

                var duplicateSince = DateTime.UtcNow - DuplicateSubmitWindow;
                duplicate = await _context.Feedbacks
                    .AsNoTracking()
                    .Where(row =>
                        row.RestaurantLocationId == location.Id
                        && row.GuestContact == guestContact
                        && row.Comment == comment
                        && row.CreatedAt >= duplicateSince
                    )
                    .OrderByDescending(row => row.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (duplicate == null)
                {
                    /*
                     =========================================
                     PER-TOKEN RATE LIMIT (10 submissions / hour)
                     =========================================
                    */

                    var rateLimitKey = $"feedback_rate:{normalizedToken}";

                    var submissions = _cache.GetOrCreate(
                        rateLimitKey,
                        entry =>
                        {
                            entry.AbsoluteExpirationRelativeToNow =
                                TimeSpan.FromHours(1);

                            return new List<DateTime>();
                        }
                    );

                    if (submissions!.Count >= 10)
                    {
                        if (transaction != null)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                        }

                        return StatusCode(
                            StatusCodes.Status429TooManyRequests,
                            new
                            {
                                success = false,
                                message = "Too many submissions from this link. Please try again later."
                            }
                        );
                    }

                    submissions.Add(DateTime.UtcNow);

                    /*
                     =========================================
                     CREATE FEEDBACK ROW (+ Master/Location Guest upsert)
                     =========================================
                    */

                    var locationGuest = await _guestUpsert.ResolveOrCreateAsync(
                        location.RestaurantId,
                        location.Id,
                        guestName,
                        guestContact,
                        contactType,
                        dto.OffersOptOut
                    );

                    var restaurant = await _context.Restaurants
                        .FirstAsync(r => r.Id == location.RestaurantId);

                    var submitAt = DateTime.UtcNow;
                    await _guestFormPermissions.ApplyOnSubmitAsync(
                        locationGuest,
                        restaurant,
                        location.Id,
                        location.LocationName,
                        !dto.OffersOptOut,
                        contactType,
                        submitAt
                    );

                    feedback = new Feedback
                    {
                        RestaurantLocationId = location.Id,
                        QrCodeId = resolution.QrCodeId,
                        LocationGuest = locationGuest,
                        GuestName = guestName,
                        GuestContact = guestContact,
                        ContactType = contactType,
                        Comment = comment,
                        OffersOptOut = dto.OffersOptOut,
                        ClassificationStatus = ClassificationStatus.Pending,
                        CreatedAt = submitAt
                    };

                    _context.Feedbacks.Add(feedback);
                    _recorder.RecordFeedback(
                        locationGuest,
                        feedback,
                        feedback.CreatedAt
                    );
                    await _context.SaveChangesAsync(cancellationToken);
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }

            if (duplicate != null)
            {
                // Same contact + comment within the window: write nothing; re-show
                // an existing thank-you Offer when LocationGuestId is present.
                // Never issue a new Claim on this path (allowNewIssue: false).
                OfferIssue? duplicateIssued = null;
                var duplicateIssueAt = DateTime.UtcNow;
                if (duplicate.LocationGuestId is int duplicateGuestId)
                {
                    try
                    {
                        duplicateIssued = await _offerIssues.IssueOnThankYouSubmitAsync(
                            location.Id,
                            duplicateGuestId,
                            duplicate.Id,
                            duplicateIssueAt,
                            cancellationToken,
                            allowNewIssue: false
                        );
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(
                            ex,
                            "Thank-you Offer re-show failed for duplicate Feedback {FeedbackId}.",
                            duplicate.Id
                        );
                        duplicateIssued = null;
                    }

                    // Concurrent winner may still be issuing after commit; poll
                    // briefly when the duplicate Feedback is fresh.
                    if (
                        duplicateIssued == null
                        && DateTime.UtcNow - duplicate.CreatedAt
                            < ConcurrentClaimWaitFreshWindow
                    )
                    {
                        duplicateIssued = await WaitForThankYouClaimAsync(
                            duplicate.Id,
                            cancellationToken
                        );
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Feedback submitted successfully.",
                    offer = duplicateIssued == null
                        ? null
                        : new ScanThankYouOfferDto
                        {
                            Title = duplicateIssued.Title,
                            Description = duplicateIssued.Description,
                            ClaimCode = duplicateIssued.ClaimCode,
                            ExpiryLabel = FeedbackRecoveryOfferMapping.FormatOfferExpiryLabel(
                                duplicateIssued.ExpiryAtUtc
                            ),
                            AlreadyClaimed = duplicateIssued.IssuedAtUtc != duplicateIssueAt,
                        },
                });
            }

            // Wake after persist — guest path never awaits the model (ADR-0010).
            await _classificationWork.NotifyAsync(feedback!.Id);

            // Catalog thank-you Issue: live Active attach issues the Standard
            // Offer independent of marketing consent. Issue failures keep the
            // Feedback and return plain success. Anti-abuse: re-show redeemable
            // codes; suppress after redeem; cap new issues per token / IP.
            OfferIssue? issued = null;
            // Outside the LocationGuestId branch so thank-you paint can compare
            // IssuedAtUtc for AlreadyClaimed after the if.
            var issueAt = DateTime.UtcNow;
            if (feedback.LocationGuestId is int locationGuestId)
            {
                var allowNewIssue = ThankYouIssueAbuseCaps.IsUnderCap(
                    _cache,
                    normalizedToken,
                    clientIp
                );

                try
                {
                    issued = await _offerIssues.IssueOnThankYouSubmitAsync(
                        location.Id,
                        locationGuestId,
                        feedback.Id,
                        issueAt,
                        allowNewIssue: allowNewIssue
                    );
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(
                        ex,
                        "Thank-you Offer issue failed after Feedback {FeedbackId}; keeping Feedback.",
                        feedback.Id
                    );
                    issued = null;
                }

                if (issued != null && issued.IssuedAtUtc == issueAt)
                {
                    ThankYouIssueAbuseCaps.RecordNewIssue(
                        _cache,
                        normalizedToken,
                        clientIp
                    );
                }
            }

            return Ok(new
            {
                success = true,
                message = "Feedback submitted successfully.",
                offer = issued == null
                    ? null
                    : new ScanThankYouOfferDto
                    {
                        Title = issued.Title,
                        Description = issued.Description,
                        ClaimCode = issued.ClaimCode,
                        ExpiryLabel = FeedbackRecoveryOfferMapping.FormatOfferExpiryLabel(
                            issued.ExpiryAtUtc
                        ),
                        AlreadyClaimed = issued.IssuedAtUtc != issueAt,
                    },
            });
        }

        /*
         =========================================
         EPHEMERAL SPEECH-TO-TEXT BY TOKEN
         Audio is transcribed in-memory and discarded — never stored.
         =========================================
        */

        [HttpPost("{token}/stt")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> TranscribeSpeech(
            string token,
            IFormFile? audio,
            CancellationToken cancellationToken
        )
        {
            var normalizedToken = token?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return NotFound(new
                {
                    success = false,
                    message = "Invalid link."
                });
            }

            var resolution = await _smartGuestLink
                .ResolveLocationForWriteAsync(normalizedToken);

            if (resolution == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Link not found."
                });
            }

            if (
                await IsDemoGuestFormAsync(
                    normalizedToken,
                    resolution.Location.RestaurantId
                )
            )
            {
                return DemoGuestFormForbidden();
            }

            if (audio == null || audio.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Audio is required."
                });
            }

            /*
             =========================================
             PER-TOKEN STT RATE LIMIT (10 attempts / hour)
             =========================================
            */

            var rateLimitKey = $"stt_rate:{normalizedToken}";

            var attempts = _cache.GetOrCreate(
                rateLimitKey,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromHours(1);

                    return new List<DateTime>();
                }
            );

            if (attempts!.Count >= 10)
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new
                    {
                        success = false,
                        message =
                            "Too many voice attempts from this link. Try typing instead."
                    }
                );
            }

            attempts.Add(DateTime.UtcNow);

            await using var audioStream = audio.OpenReadStream();
            var result = await _speechToText.TranscribeAsync(
                audioStream,
                audio.ContentType ?? "application/octet-stream",
                cancellationToken
            );

            return result switch
            {
                SpeechToTextResult.Succeeded succeeded => Ok(new
                {
                    success = true,
                    text = TruncateTranscript(succeeded.Text)
                }),
                SpeechToTextResult.EmptySpeech => UnprocessableEntity(new
                {
                    success = false,
                    code = "empty_speech",
                    message =
                        "We didn't catch any speech. Try again or type your feedback."
                }),
                _ => StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        code = "stt_failure",
                        message =
                            "We couldn't transcribe that recording. Try again or type your feedback."
                    }
                )
            };
        }

        /// <summary>
        /// Poll for a thank-you OfferIssue the concurrent winner may still be
        /// writing. Write-free: never creates a Claim.
        /// </summary>
        private async Task<OfferIssue?> WaitForThankYouClaimAsync(
            int feedbackId,
            CancellationToken cancellationToken
        )
        {
            var deadline = DateTime.UtcNow + ConcurrentClaimPollBudget;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var issue = await _context.OfferIssues
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        row => row.FeedbackId == feedbackId,
                        cancellationToken
                    );
                if (issue != null)
                {
                    return issue;
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    return null;
                }

                var delay =
                    ConcurrentClaimPollStep < remaining
                        ? ConcurrentClaimPollStep
                        : remaining;
                await Task.Delay(delay, cancellationToken);
            }
        }

        private static string TruncateTranscript(string text)
        {
            var trimmed = text.Trim();
            return trimmed.Length <= 1000
                ? trimmed
                : trimmed[..1000];
        }

        private async Task<Restaurant> LoadRestaurantForLocationAsync(
            int locationId
        )
        {
            var restaurantId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => row.RestaurantId)
                .FirstAsync();

            return await _context.Restaurants
                .AsNoTracking()
                .FirstAsync(r => r.Id == restaurantId);
        }

        private static GuestFormConsentDto BuildGuestFormConsentDto(
            Restaurant restaurant
        )
        {
            return new GuestFormConsentDto
            {
                EmailMarketingEnabled =
                    restaurant.EmailMarketingPermissionEnabled,
                SmsMarketingEnabled = restaurant.SmsMarketingPermissionEnabled,
                FeedbackFollowUpEnabled =
                    restaurant.FeedbackFollowUpPermissionEnabled,
                EmailConsentWording = restaurant.EmailConsentWording,
                SmsConsentWording = restaurant.SmsConsentWording,
                FeedbackFollowUpWording =
                    GuestFormConsentCopy.FeedbackFollowUpWording,
            };
        }

        private DemoSessionOptions DemoOptions()
            => _configuration
                .GetSection(DemoSessionOptions.SectionName)
                .Get<DemoSessionOptions>()
            ?? new DemoSessionOptions();

        /// <summary>
        /// The seeded Mavero token is always demo, even if OwnerEmail is unset
        /// (fail closed).
        /// </summary>
        private static bool IsMaveroDemoToken(string token)
            => string.Equals(
                token,
                DemoSessionClaims.MaveroTableTentToken,
                StringComparison.OrdinalIgnoreCase
            );

        private async Task<bool> IsDemoGuestFormAsync(string token, int restaurantId)
            => IsMaveroDemoToken(token)
                || await DemoRestaurant.IsDemoRestaurantAsync(
                    _context,
                    DemoOptions(),
                    restaurantId,
                    HttpContext.RequestAborted
                );

        private ObjectResult DemoGuestFormForbidden()
            => StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    success = false,
                    message = DemoRestaurant.NothingSavedMessage,
                }
            );
    }
}
