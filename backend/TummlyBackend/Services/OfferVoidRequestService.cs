using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Notifications;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Void request persistence — create / approve / reject (ticket 39).
    /// </summary>
    public sealed class OfferVoidRequestService : IOfferVoidRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOperatorNotificationsService _notifications;

        private static readonly Dictionary<string, string> ReasonLabels =
            new(StringComparer.Ordinal)
            {
                [OfferVoidRequestReasonIds.RedeemedByMistake] =
                    "Redeemed by mistake",
                [OfferVoidRequestReasonIds.WrongOfferPass] =
                    "Wrong offer pass was used",
                [OfferVoidRequestReasonIds.DuplicateRedemption] =
                    "Duplicate redemption recorded",
                [OfferVoidRequestReasonIds.GuestDidNotReceive] =
                    "Guest did not receive the benefit",
                [OfferVoidRequestReasonIds.IncorrectLocation] =
                    "Incorrect location recorded",
                [OfferVoidRequestReasonIds.Other] = "Other",
            };

        private static readonly Dictionary<string, string> CorrectionLabels =
            new(StringComparer.Ordinal)
            {
                [OfferVoidRequestCorrectionIds.KeepUnusable] =
                    "Keep pass unusable",
                [OfferVoidRequestCorrectionIds.RestoreOneUse] =
                    "Restore one redemption use",
            };

        public OfferVoidRequestService(
            ApplicationDbContext context,
            IOperatorNotificationsService notifications
        )
        {
            _context = context;
            _notifications = notifications;
        }

        public async Task<OfferVoidCreateResult> CreateAsync(
            int userId,
            CreateOfferVoidRequestBody body,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            if (!OfferVoidRequestReasonIds.All.Contains(body.ReasonId))
            {
                return new OfferVoidCreateResult
                {
                    Status = OfferVoidCreateResultStatus.Invalid,
                };
            }

            if (!OfferVoidRequestCorrectionIds.All.Contains(body.CorrectionId))
            {
                return new OfferVoidCreateResult
                {
                    Status = OfferVoidCreateResultStatus.Invalid,
                };
            }

            var explanation = body.Explanation?.Trim();
            if (body.ReasonId == OfferVoidRequestReasonIds.Other
                && string.IsNullOrEmpty(explanation))
            {
                return new OfferVoidCreateResult
                {
                    Status = OfferVoidCreateResultStatus.Invalid,
                };
            }

            if (body.ReasonId != OfferVoidRequestReasonIds.Other)
            {
                explanation = string.IsNullOrEmpty(explanation) ? null : explanation;
            }

            var issue = await _context.OfferIssues
                .Include(i => i.LocationGuest)!
                    .ThenInclude(g => g!.RestaurantLocation)
                .Include(i => i.Campaign)
                .FirstOrDefaultAsync(i => i.Id == body.IssueId, cancellationToken);

            if (issue == null
                || issue.CatalogOfferId != body.OfferId
                || issue.LocationGuest?.RestaurantLocationId != body.LocationId)
            {
                return new OfferVoidCreateResult
                {
                    Status = OfferVoidCreateResultStatus.NotFound,
                };
            }

            if (issue.RedeemedAtUtc == null)
            {
                return new OfferVoidCreateResult
                {
                    Status = OfferVoidCreateResultStatus.NotRedeemed,
                };
            }

            if (issue.RedemptionVoidedAtUtc != null)
            {
                return new OfferVoidCreateResult
                {
                    Status = OfferVoidCreateResultStatus.Invalid,
                };
            }

            var pendingExists = await _context.OfferVoidRequests.AnyAsync(
                row =>
                    row.OfferIssueId == issue.Id
                    && row.Status == OfferVoidRequestStatuses.Pending,
                cancellationToken
            );
            if (pendingExists)
            {
                return new OfferVoidCreateResult
                {
                    Status = OfferVoidCreateResultStatus.PendingExists,
                };
            }

            var entity = new OfferVoidRequest
            {
                OfferIssueId = issue.Id,
                CatalogOfferId = issue.CatalogOfferId,
                RestaurantLocationId = body.LocationId,
                RequestedByUserId = userId,
                RequestedAtUtc = atUtc,
                OriginalRedeemedAtUtc = issue.RedeemedAtUtc.Value,
                ReasonId = body.ReasonId,
                Explanation = explanation,
                CorrectionId = body.CorrectionId,
                Status = OfferVoidRequestStatuses.Pending,
            };

            _context.OfferVoidRequests.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return new OfferVoidCreateResult
            {
                Status = OfferVoidCreateResultStatus.Created,
                RequestId = entity.Id.ToString(CultureInfo.InvariantCulture),
            };
        }

        public async Task<OfferVoidOutcomeResult> ApproveAsync(
            int userId,
            int requestId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            var request = await LoadPendingWithIssueAsync(requestId, cancellationToken);
            if (request == null)
            {
                return new OfferVoidOutcomeResult
                {
                    Status = OfferVoidOutcomeResultStatus.NotFound,
                };
            }

            if (request.Status != OfferVoidRequestStatuses.Pending)
            {
                return new OfferVoidOutcomeResult
                {
                    Status = OfferVoidOutcomeResultStatus.NotPending,
                };
            }

            var issue = request.OfferIssue!;
            if (request.CorrectionId
                == OfferVoidRequestCorrectionIds.KeepUnusable)
            {
                issue.RedemptionVoidedAtUtc = atUtc;
                issue.CancelledAtUtc = atUtc;
            }
            else if (request.CorrectionId
                == OfferVoidRequestCorrectionIds.RestoreOneUse)
            {
                issue.RedeemedAtUtc = null;
            }

            request.Status = OfferVoidRequestStatuses.Approved;
            request.ResolvedByUserId = userId;
            request.ResolvedAtUtc = atUtc;

            await _context.SaveChangesAsync(cancellationToken);

            return new OfferVoidOutcomeResult
            {
                Status = OfferVoidOutcomeResultStatus.Ok,
            };
        }

        public async Task<OfferVoidOutcomeResult> RejectAsync(
            int userId,
            int requestId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            var request = await _context.OfferVoidRequests
                .FirstOrDefaultAsync(row => row.Id == requestId, cancellationToken);

            if (request == null)
            {
                return new OfferVoidOutcomeResult
                {
                    Status = OfferVoidOutcomeResultStatus.NotFound,
                };
            }

            if (request.Status != OfferVoidRequestStatuses.Pending)
            {
                return new OfferVoidOutcomeResult
                {
                    Status = OfferVoidOutcomeResultStatus.NotPending,
                };
            }

            request.Status = OfferVoidRequestStatuses.Rejected;
            request.ResolvedByUserId = userId;
            request.ResolvedAtUtc = atUtc;

            await _context.SaveChangesAsync(cancellationToken);

            return new OfferVoidOutcomeResult
            {
                Status = OfferVoidOutcomeResultStatus.Ok,
            };
        }

        public async Task<OfferVoidRequestDetailDto?> GetDetailAsync(
            int requestId,
            CancellationToken cancellationToken = default
        )
        {
            var request = await _context.OfferVoidRequests
                .AsNoTracking()
                .Include(row => row.OfferIssue)!
                    .ThenInclude(i => i!.LocationGuest)!
                        .ThenInclude(g => g!.RestaurantLocation)
                .Include(row => row.OfferIssue)!.ThenInclude(i => i!.Campaign)
                .Include(row => row.RequestedByUser)
                .Include(row => row.CatalogOffer)
                .FirstOrDefaultAsync(row => row.Id == requestId, cancellationToken);

            if (request?.OfferIssue == null)
            {
                return null;
            }

            return MapDetail(request, request.OfferIssue);
        }

        public async Task<IReadOnlyList<OpenVoidAttentionOfferDto>> ListOpenAttentionAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await (
                from request in _context.OfferVoidRequests.AsNoTracking()
                join offer in _context.CatalogOffers.AsNoTracking()
                    on request.CatalogOfferId equals offer.Id
                where request.RestaurantLocationId == locationId
                    && request.Status == OfferVoidRequestStatuses.Pending
                group request by new { request.CatalogOfferId, offer.Title }
                into grouped
                select new OpenVoidAttentionOfferDto
                {
                    OfferId = grouped.Key.CatalogOfferId,
                    OfferTitle = grouped.Key.Title,
                    PendingCount = grouped.Count(),
                }
            )
                .OrderBy(row => row.OfferTitle)
                .ToListAsync(cancellationToken);

            return rows;
        }

        public async Task<OfferDetailsVoidRequestsListDto?> ListForOfferAsync(
            int offerId,
            CancellationToken cancellationToken = default
        )
        {
            var offerExists = await _context.CatalogOffers
                .AsNoTracking()
                .AnyAsync(o => o.Id == offerId, cancellationToken);

            if (!offerExists)
            {
                return null;
            }

            var requests = await _context.OfferVoidRequests
                .AsNoTracking()
                .Where(row => row.CatalogOfferId == offerId)
                .Include(row => row.OfferIssue)!
                    .ThenInclude(i => i!.LocationGuest)!
                        .ThenInclude(g => g!.RestaurantLocation)
                .Include(row => row.OfferIssue)!
                    .ThenInclude(i => i!.Campaign)
                .Include(row => row.RequestedByUser)
                .Include(row => row.CatalogOffer)
                .Include(row => row.RestaurantLocation)
                .OrderByDescending(row => row.RequestedAtUtc)
                .ThenByDescending(row => row.Id)
                .ToListAsync(cancellationToken);

            var items = requests
                .Select(ToListItem)
                .ToList();

            return new OfferDetailsVoidRequestsListDto { Items = items };
        }

        public Task NotifyApproversAsync(
            int requestId,
            CancellationToken cancellationToken = default
        ) =>
            NotifyApproversInternalAsync(requestId, cancellationToken);

        public Task NotifySubmitterAsync(
            int requestId,
            string outcome,
            CancellationToken cancellationToken = default
        ) =>
            NotifySubmitterInternalAsync(requestId, outcome, cancellationToken);

        private async Task<OfferVoidRequest?> LoadPendingWithIssueAsync(
            int requestId,
            CancellationToken cancellationToken
        )
        {
            return await _context.OfferVoidRequests
                .Include(row => row.OfferIssue)
                .FirstOrDefaultAsync(row => row.Id == requestId, cancellationToken);
        }

        private OfferVoidRequestDetailDto MapDetail(
            OfferVoidRequest request,
            OfferIssue issue
        )
        {
            var guestName = issue.LocationGuest?.Name?.Trim() ?? string.Empty;
            var locationName =
                issue.LocationGuest?.RestaurantLocation?.LocationName?.Trim()
                ?? string.Empty;
            var requestedBy =
                request.RequestedByUser?.FullName?.Trim() ?? "Operator";

            return new OfferVoidRequestDetailDto
            {
                RequestId = request.Id.ToString(CultureInfo.InvariantCulture),
                PassId = issue.Id.ToString(CultureInfo.InvariantCulture),
                OfferId = request.CatalogOfferId,
                LocationId = request.RestaurantLocationId,
                OfferTitle = issue.Title,
                GuestName = guestName,
                PassCodeMasked = MaskClaimCode(issue.ClaimCode),
                CurrentStateText = ResolveCurrentStateText(issue, request),
                ExpiresText = FormatExpiresLabel(issue.ExpiryAtUtc),
                LocationName = locationName,
                LinkedCampaignText = issue.Campaign?.Name?.Trim()
                    ?? "Not issued through a campaign",
                RequestedByText = requestedBy,
                RequestedAtText = FormatRequestedAtLabel(request.RequestedAtUtc),
                ReasonId = request.ReasonId,
                ReasonText = ReasonLabels.GetValueOrDefault(
                    request.ReasonId,
                    request.ReasonId
                ),
                Explanation = request.Explanation,
                CorrectionId = request.CorrectionId,
                CorrectionText = CorrectionLabels.GetValueOrDefault(
                    request.CorrectionId,
                    request.CorrectionId
                ),
            };
        }

        private OfferDetailsVoidRequestListItemDto ToListItem(
            OfferVoidRequest request
        )
        {
            if (request.OfferIssue != null)
            {
                return ToListItem(request, request.OfferIssue);
            }

            var requestedBy =
                request.RequestedByUser?.FullName?.Trim() ?? "Operator";
            var locationName =
                request.RestaurantLocation?.LocationName?.Trim()
                ?? string.Empty;
            var offerTitle = request.CatalogOffer?.Title?.Trim() ?? string.Empty;

            return new OfferDetailsVoidRequestListItemDto
            {
                RequestId = request.Id.ToString(CultureInfo.InvariantCulture),
                RequestedAtUtc = request.RequestedAtUtc,
                RequestedAtText = FormatRequestedAtLabel(request.RequestedAtUtc),
                RequestedByText = requestedBy,
                GuestName = string.Empty,
                OfferPassText = "—",
                ReasonId = request.ReasonId,
                ReasonText = ReasonLabels.GetValueOrDefault(
                    request.ReasonId,
                    request.ReasonId
                ),
                Explanation = request.Explanation,
                LocationName = locationName,
                CurrentStateText = "—",
                CorrectionId = request.CorrectionId,
                CorrectionText = CorrectionLabels.GetValueOrDefault(
                    request.CorrectionId,
                    request.CorrectionId
                ),
                Status = request.Status,
                StatusLabel = FormatVoidStatusLabel(request.Status),
                PassId = request.OfferIssueId.ToString(
                    CultureInfo.InvariantCulture
                ),
                PassCodeMasked = "—",
                ExpiresText = "—",
                LinkedCampaignText = "Not issued through a campaign",
                OfferTitle = offerTitle,
            };
        }

        private OfferDetailsVoidRequestListItemDto ToListItem(
            OfferVoidRequest request,
            OfferIssue issue
        )
        {
            var detail = MapDetail(request, issue);
            return new OfferDetailsVoidRequestListItemDto
            {
                RequestId = detail.RequestId,
                RequestedAtUtc = request.RequestedAtUtc,
                RequestedAtText = detail.RequestedAtText,
                RequestedByText = detail.RequestedByText,
                GuestName = detail.GuestName,
                OfferPassText = detail.PassCodeMasked,
                ReasonId = detail.ReasonId,
                ReasonText = detail.ReasonText,
                Explanation = detail.Explanation,
                LocationName = detail.LocationName,
                CurrentStateText = detail.CurrentStateText,
                CorrectionId = detail.CorrectionId,
                CorrectionText = detail.CorrectionText,
                Status = request.Status,
                StatusLabel = FormatVoidStatusLabel(request.Status),
                PassId = detail.PassId,
                PassCodeMasked = detail.PassCodeMasked,
                ExpiresText = detail.ExpiresText,
                LinkedCampaignText = detail.LinkedCampaignText,
                OfferTitle = detail.OfferTitle,
            };
        }

        private static string FormatVoidStatusLabel(string status)
        {
            if (string.Equals(
                    status,
                    OfferVoidRequestStatuses.Pending,
                    StringComparison.Ordinal
                ))
            {
                return "Pending";
            }

            if (string.Equals(
                    status,
                    OfferVoidRequestStatuses.Approved,
                    StringComparison.Ordinal
                ))
            {
                return "Approved";
            }

            if (string.Equals(
                    status,
                    OfferVoidRequestStatuses.Rejected,
                    StringComparison.Ordinal
                ))
            {
                return "Rejected";
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return "—";
            }

            return char.ToUpperInvariant(status[0]) + status[1..];
        }

        private static string ResolveCurrentStateText(
            OfferIssue issue,
            OfferVoidRequest request
        )
        {
            if (request.Status == OfferVoidRequestStatuses.Pending)
            {
                return issue.RedeemedAtUtc != null ? "Redeemed" : "Claimed";
            }

            return issue.RedeemedAtUtc != null ? "Redeemed" : "Available";
        }

        private async Task NotifyApproversInternalAsync(
            int requestId,
            CancellationToken cancellationToken
        )
        {
            var request = await _context.OfferVoidRequests
                .AsNoTracking()
                .Include(row => row.CatalogOffer)!
                    .ThenInclude(o => o!.RestaurantLocation)!
                        .ThenInclude(l => l!.Restaurant)
                .Include(row => row.RequestedByUser)
                .FirstOrDefaultAsync(row => row.Id == requestId, cancellationToken);

            if (request?.CatalogOffer?.RestaurantLocation?.Restaurant == null)
            {
                return;
            }

            var ownerUserId =
                request.CatalogOffer.RestaurantLocation.Restaurant.OwnerUserId;
            if (ownerUserId == request.RequestedByUserId)
            {
                return;
            }

            var submitter =
                request.RequestedByUser?.FullName?.Trim() ?? "A team member";
            var offerTitle = request.CatalogOffer.Title.Trim();
            var locationId = request.RestaurantLocationId;
            var offerId = request.CatalogOfferId;

            await TryProduceNoticeAsync(
                ownerUserId,
                "offer-void-request-pending",
                "Void request needs review",
                $"{submitter} requested a void correction for “{offerTitle}”.",
                ctaLabel: "Review void request",
                ctaHref:
                    $"/single-dashboard/offers/{offerId}?location={locationId}&tab=void-requests",
                dedupeKey: $"void-pending:{requestId}"
            );
        }

        private async Task NotifySubmitterInternalAsync(
            int requestId,
            string outcome,
            CancellationToken cancellationToken
        )
        {
            var request = await _context.OfferVoidRequests
                .AsNoTracking()
                .Include(row => row.CatalogOffer)
                .FirstOrDefaultAsync(row => row.Id == requestId, cancellationToken);

            if (request?.CatalogOffer == null)
            {
                return;
            }

            var approved = string.Equals(
                outcome,
                "approved",
                StringComparison.OrdinalIgnoreCase
            );
            var offerTitle = request.CatalogOffer.Title.Trim();
            var locationId = request.RestaurantLocationId;
            var offerId = request.CatalogOfferId;

            await TryProduceNoticeAsync(
                request.RequestedByUserId,
                "offer-void-request-outcome",
                approved ? "Void request approved" : "Void request rejected",
                approved
                    ? $"Your void request for “{offerTitle}” was approved."
                    : $"Your void request for “{offerTitle}” was rejected.",
                ctaLabel: "View void requests",
                ctaHref:
                    $"/single-dashboard/offers/{offerId}?location={locationId}&tab=void-requests",
                dedupeKey: $"void-outcome:{requestId}:{outcome.ToLowerInvariant()}"
            );
        }

        private async Task TryProduceNoticeAsync(
            int userId,
            string type,
            string title,
            string body,
            string ctaLabel,
            string ctaHref,
            string dedupeKey
        )
        {
            try
            {
                await _notifications.ProduceAsync(
                    new ProduceNotificationRequest
                    {
                        UserId = userId,
                        Type = type,
                        Title = title,
                        Body = body,
                        CtaLabel = ctaLabel,
                        CtaHref = ctaHref,
                        DedupeKey = dedupeKey,
                    }
                );
            }
            catch
            {
                // Notification failure must not roll back void writes.
            }
        }

        private static string MaskClaimCode(string claimCode)
        {
            var trimmed = claimCode.Trim();
            if (trimmed.Length <= 4)
            {
                return trimmed;
            }

            var suffix = trimmed[^4..];
            return $"•••• {suffix}";
        }

        private static string FormatExpiresLabel(DateTime expiryAtUtc)
        {
            return expiryAtUtc
                .ToString(
                    "d MMMM yyyy",
                    CultureInfo.GetCultureInfo("en-GB")
                );
        }

        private static string FormatRequestedAtLabel(DateTime requestedAtUtc)
        {
            return requestedAtUtc
                .ToString(
                    "d MMM yyyy, h:mmtt",
                    CultureInfo.GetCultureInfo("en-GB")
                )
                .Replace("AM", "am", StringComparison.Ordinal)
                .Replace("PM", "pm", StringComparison.Ordinal);
        }
    }
}
