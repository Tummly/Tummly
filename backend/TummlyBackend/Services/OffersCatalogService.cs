using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Offers catalog create / get / list / lifecycle (ticket 22).
    /// Not Feedback recovery-offer issue APIs.
    /// </summary>
    public class OffersCatalogService : IOffersCatalogService
    {
        public const string ActiveStatus = CatalogOfferStatus.Active;
        public const int MaxTitleLength = 60;
        public const int MaxDescriptionLength = 240;
        public const int MaxStaffInstructionsLength = 1000;
        public const int DefaultPageSize = CampaignsListService.DefaultPageSize;

        private static readonly HashSet<string> AllowedViews = new(
            StringComparer.Ordinal
        )
        {
            "all",
            "needs-attention",
            "drafts",
            "in-flight",
            "sent",
        };

        private static readonly HashSet<string> AllowedSorts = new(
            StringComparer.Ordinal
        )
        {
            "recent-activity",
            "title-az",
        };

        private static readonly HashSet<string> AllowedStatuses = new(
            StringComparer.Ordinal
        )
        {
            CatalogOfferStatus.Draft,
            CatalogOfferStatus.Active,
            CatalogOfferStatus.Paused,
            CatalogOfferStatus.Expired,
            CatalogOfferStatus.Archived,
        };

        private static readonly HashSet<string> AllowedAttachSources = new(
            StringComparer.Ordinal
        )
        {
            CatalogOfferStatus.AttachSourceCampaign,
            CatalogOfferStatus.AttachSourceRecovery,
            CatalogOfferStatus.AttachSourceGuestFormThankYou,
            CatalogOfferStatus.AttachSourceManual,
        };

        private readonly ApplicationDbContext _context;
        private readonly Func<DateTime> _utcNow;

        public OffersCatalogService(
            ApplicationDbContext context,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<CatalogOfferDto> CreateActiveAsync(
            CreateCatalogOfferRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (request.LocationId < 1)
            {
                throw new ArgumentException("locationId is required.");
            }

            var fields = ParseAndValidateFields(request);
            var now = _utcNow();

            var entity = new CatalogOffer
            {
                RestaurantLocationId = request.LocationId,
                Status = ActiveStatus,
                OfferType = fields.OfferType,
                Title = fields.Title,
                Description = fields.Description,
                Validity = fields.Validity,
                CustomExpiryDate = fields.CustomExpiryDate,
                DiscountPercentage = fields.DiscountPercentage,
                DiscountAmount = fields.DiscountAmount,
                FreeItemText = fields.FreeItemText,
                PurchaseRequirement = fields.PurchaseRequirement,
                MinimumSpend = fields.MinimumSpend,
                AdditionalExclusions = fields.AdditionalExclusions,
                ReplacementItemText = fields.ReplacementItemText,
                StaffInstructions = fields.StaffInstructions,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _context.CatalogOffers.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(entity, utcOffsetMinutes: 0, issueCount: 0);
        }

        public async Task<CatalogOfferLifecycleResult> UpdateAsync(
            int offerId,
            CreateCatalogOfferRequest request,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await LoadForMutationAsync(offerId, cancellationToken);
            if (entity == null)
            {
                return new CatalogOfferLifecycleResult.NotFound();
            }

            var today = CatalogOfferStatus.VenueLocalToday(
                _utcNow(),
                utcOffsetMinutes
            );
            var effective = CatalogOfferStatus.ResolveEffectiveStatus(
                entity.Status,
                entity.Validity,
                entity.CustomExpiryDate,
                today
            );

            var editable =
                string.Equals(entity.Status, CatalogOfferStatus.Draft, StringComparison.Ordinal)
                || string.Equals(entity.Status, CatalogOfferStatus.Active, StringComparison.Ordinal)
                || string.Equals(entity.Status, CatalogOfferStatus.Paused, StringComparison.Ordinal);

            if (!editable
                || string.Equals(effective, CatalogOfferStatus.Expired, StringComparison.Ordinal)
                || string.Equals(effective, CatalogOfferStatus.Archived, StringComparison.Ordinal)
                || string.Equals(entity.Status, CatalogOfferStatus.Archived, StringComparison.Ordinal))
            {
                return new CatalogOfferLifecycleResult.InvalidStatus
                {
                    Message =
                        "Edit is only allowed for Draft, Active, or Paused offers.",
                };
            }

            if (!CatalogOfferMapping.TryParseOfferType(
                    request.OfferType,
                    out var requestType
                ))
            {
                throw new ArgumentException("Offer type is invalid.");
            }

            if (requestType != entity.OfferType)
            {
                throw new ArgumentException("Offer type cannot be changed.");
            }

            var fields = ParseAndValidateFields(request);

            entity.Title = fields.Title;
            entity.Description = fields.Description;
            entity.Validity = fields.Validity;
            entity.CustomExpiryDate = fields.CustomExpiryDate;
            entity.DiscountPercentage = fields.DiscountPercentage;
            entity.DiscountAmount = fields.DiscountAmount;
            entity.FreeItemText = fields.FreeItemText;
            entity.PurchaseRequirement = fields.PurchaseRequirement;
            entity.MinimumSpend = fields.MinimumSpend;
            entity.AdditionalExclusions = fields.AdditionalExclusions;
            entity.ReplacementItemText = fields.ReplacementItemText;
            entity.StaffInstructions = fields.StaffInstructions;
            entity.UpdatedAt = _utcNow();

            await _context.SaveChangesAsync(cancellationToken);

            var issueCount = await CountIssuesAsync(offerId, cancellationToken);
            return new CatalogOfferLifecycleResult.Ok
            {
                Offer = ToDto(entity, utcOffsetMinutes, issueCount),
            };
        }

        public async Task<CatalogOfferDto?> GetByIdAsync(
            int offerId,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await _context.CatalogOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(offer => offer.Id == offerId, cancellationToken);

            if (entity == null)
            {
                return null;
            }

            var issueCount = await CountIssuesAsync(offerId, cancellationToken);
            return ToDto(entity, utcOffsetMinutes, issueCount);
        }

        public async Task<bool> IsActiveForLocationAsync(
            int offerId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await _context.CatalogOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    offer =>
                        offer.Id == offerId
                        && offer.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (entity == null)
            {
                return false;
            }

            var today = CatalogOfferStatus.VenueLocalToday(_utcNow(), 0);
            return CatalogOfferStatus.IsAttachableActive(
                entity.Status,
                entity.Validity,
                entity.CustomExpiryDate,
                today
            );
        }

        public async Task<CatalogOffersListResponse> ListAsync(
            CatalogOffersListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ValidatePaging(query.Page, query.PageSize);
            var view = NormalizeView(query.View);
            var sort = NormalizeSort(query.Sort);
            var nameQuery = query.Q?.Trim() ?? string.Empty;
            var statuses = NormalizeStringFilters(
                query.Status,
                AllowedStatuses,
                "status"
            );
            var attachSources = NormalizeStringFilters(
                query.AttachSource,
                AllowedAttachSources,
                "attachSource"
            );

            var today = CatalogOfferStatus.VenueLocalToday(
                _utcNow(),
                query.UtcOffsetMinutes
            );

            var offers = await _context.CatalogOffers
                .AsNoTracking()
                .Where(offer => offer.RestaurantLocationId == query.LocationId)
                .ToListAsync(cancellationToken);

            var offerIds = offers.Select(o => o.Id).ToList();
            var campaignAttaches = offerIds.Count == 0
                ? new List<(int OfferId, string Name)>()
                : (await _context.Campaigns
                    .AsNoTracking()
                    .Where(c =>
                        c.OfferId != null
                        && offerIds.Contains(c.OfferId.Value)
                    )
                    .Select(c => new { OfferId = c.OfferId!.Value, c.Name })
                    .ToListAsync(cancellationToken))
                    .Select(row => (row.OfferId, row.Name))
                    .ToList();

            var lifetimeByOffer = offerIds.Count == 0
                ? new Dictionary<int, (int Claims, int Redeemed)>()
                : (await _context.OfferIssues
                    .AsNoTracking()
                    .Where(i => offerIds.Contains(i.CatalogOfferId))
                    .GroupBy(i => i.CatalogOfferId)
                    .Select(g => new
                    {
                        OfferId = g.Key,
                        Claims = g.Count(i => i.ClaimedAtUtc != null),
                        Redeemed = g.Count(i => i.RedeemedAtUtc != null),
                    })
                    .ToListAsync(cancellationToken))
                    .ToDictionary(
                        row => row.OfferId,
                        row => (row.Claims, row.Redeemed)
                    );

            var campaignsByOffer = campaignAttaches
                .GroupBy(row => row.OfferId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(row => row.Name).ToList()
                );

            var projected = offers
                .Select(offer =>
                {
                    var campaignNames = campaignsByOffer.TryGetValue(
                        offer.Id,
                        out var names
                    )
                        ? names
                        : new List<string>();
                    var liveAttachCount = campaignNames.Count;
                    var effective = CatalogOfferStatus.ResolveEffectiveStatus(
                        offer.Status,
                        offer.Validity,
                        offer.CustomExpiryDate,
                        today
                    );
                    var attachKinds = liveAttachCount > 0
                        ? new[] { CatalogOfferStatus.AttachKindCampaign }
                        : Array.Empty<string>();
                    // Void persistence not shipped yet — hasOpenVoidRequest stays
                    // false until OfferVoidRequest (or equivalent) is queryable.
                    var needsAttention =
                        CatalogOfferStatus.IsNeedsAttention(
                            offer.Validity,
                            offer.CustomExpiryDate,
                            effective,
                            today,
                            hasOpenVoidRequest: false
                        );
                    var lifetime = lifetimeByOffer.TryGetValue(
                        offer.Id,
                        out var counts
                    )
                        ? counts
                        : (Claims: 0, Redeemed: 0);

                    return new OfferProjection(
                        offer,
                        effective,
                        liveAttachCount,
                        attachKinds,
                        campaignNames,
                        needsAttention,
                        lifetime.Claims,
                        lifetime.Redeemed
                    );
                })
                .ToList();

            var tabCounts = new CatalogOffersTabCountsDto
            {
                All = projected.Count,
                Drafts = projected.Count(row =>
                    CatalogOfferStatus.MatchesView(
                        "drafts",
                        row.Entity.Status,
                        row.EffectiveStatus,
                        row.LiveAttachCount
                    )
                ),
                InFlight = projected.Count(row =>
                    CatalogOfferStatus.MatchesView(
                        "in-flight",
                        row.Entity.Status,
                        row.EffectiveStatus,
                        row.LiveAttachCount
                    )
                ),
                Sent = projected.Count(row =>
                    CatalogOfferStatus.MatchesView(
                        "sent",
                        row.Entity.Status,
                        row.EffectiveStatus,
                        row.LiveAttachCount
                    )
                ),
                NeedsAttention = projected.Count(row => row.NeedsAttention),
            };

            IEnumerable<OfferProjection> filtered = projected;

            filtered = view switch
            {
                "needs-attention" => filtered.Where(row => row.NeedsAttention),
                "drafts" => filtered.Where(row =>
                    CatalogOfferStatus.MatchesView(
                        "drafts",
                        row.Entity.Status,
                        row.EffectiveStatus,
                        row.LiveAttachCount
                    )
                ),
                "in-flight" => filtered.Where(row =>
                    CatalogOfferStatus.MatchesView(
                        "in-flight",
                        row.Entity.Status,
                        row.EffectiveStatus,
                        row.LiveAttachCount
                    )
                ),
                "sent" => filtered.Where(row =>
                    CatalogOfferStatus.MatchesView(
                        "sent",
                        row.Entity.Status,
                        row.EffectiveStatus,
                        row.LiveAttachCount
                    )
                ),
                _ => filtered,
            };

            if (statuses.Count > 0)
            {
                filtered = filtered.Where(row =>
                    statuses.Contains(row.EffectiveStatus)
                );
            }

            if (attachSources.Count > 0)
            {
                filtered = filtered.Where(row =>
                    MatchesAttachSources(row, attachSources)
                );
            }

            if (nameQuery.Length > 0)
            {
                var needle = nameQuery;
                filtered = filtered.Where(row =>
                    row.Entity.Title.Contains(
                        needle,
                        StringComparison.OrdinalIgnoreCase
                    )
                    || row.CampaignNames.Any(name =>
                        name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    )
                );
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;

            IEnumerable<OfferProjection> ordered = sort switch
            {
                "title-az" => filteredList
                    .OrderBy(row => row.Entity.Title, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(row => row.Entity.Id),
                _ => filteredList
                    .OrderByDescending(row => row.Entity.UpdatedAt)
                    .ThenByDescending(row => row.Entity.Id),
            };

            var pageRows = ordered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(row => ToListItem(row))
                .ToList();

            return new CatalogOffersListResponse
            {
                Items = pageRows,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                TabCounts = tabCounts,
            };
        }

        public async Task<CatalogOfferLifecycleResult> PauseAsync(
            int offerId,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await LoadForMutationAsync(offerId, cancellationToken);
            if (entity == null)
            {
                return new CatalogOfferLifecycleResult.NotFound();
            }

            var today = CatalogOfferStatus.VenueLocalToday(
                _utcNow(),
                utcOffsetMinutes
            );
            var effective = CatalogOfferStatus.ResolveEffectiveStatus(
                entity.Status,
                entity.Validity,
                entity.CustomExpiryDate,
                today
            );

            if (!string.Equals(effective, CatalogOfferStatus.Active, StringComparison.Ordinal)
                || !string.Equals(entity.Status, CatalogOfferStatus.Active, StringComparison.Ordinal))
            {
                return new CatalogOfferLifecycleResult.InvalidStatus
                {
                    Message = "Pause is only allowed from Active offers.",
                };
            }

            entity.Status = CatalogOfferStatus.Paused;
            entity.UpdatedAt = _utcNow();
            await _context.SaveChangesAsync(cancellationToken);
            var issueCount = await CountIssuesAsync(offerId, cancellationToken);
            return new CatalogOfferLifecycleResult.Ok
            {
                Offer = ToDto(entity, utcOffsetMinutes, issueCount),
            };
        }

        public async Task<CatalogOfferLifecycleResult> ResumeAsync(
            int offerId,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await LoadForMutationAsync(offerId, cancellationToken);
            if (entity == null)
            {
                return new CatalogOfferLifecycleResult.NotFound();
            }

            if (!string.Equals(
                    entity.Status,
                    CatalogOfferStatus.Paused,
                    StringComparison.Ordinal
                ))
            {
                return new CatalogOfferLifecycleResult.InvalidStatus
                {
                    Message = "Resume is only allowed from Paused offers.",
                };
            }

            entity.Status = CatalogOfferStatus.Active;
            entity.UpdatedAt = _utcNow();
            await _context.SaveChangesAsync(cancellationToken);
            var issueCount = await CountIssuesAsync(offerId, cancellationToken);
            return new CatalogOfferLifecycleResult.Ok
            {
                Offer = ToDto(entity, utcOffsetMinutes, issueCount),
            };
        }

        public async Task<CatalogOfferLifecycleResult> ArchiveAsync(
            int offerId,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await LoadForMutationAsync(offerId, cancellationToken);
            if (entity == null)
            {
                return new CatalogOfferLifecycleResult.NotFound();
            }

            var today = CatalogOfferStatus.VenueLocalToday(
                _utcNow(),
                utcOffsetMinutes
            );
            var effective = CatalogOfferStatus.ResolveEffectiveStatus(
                entity.Status,
                entity.Validity,
                entity.CustomExpiryDate,
                today
            );

            if (string.Equals(entity.Status, CatalogOfferStatus.Archived, StringComparison.Ordinal))
            {
                return new CatalogOfferLifecycleResult.InvalidStatus
                {
                    Message = "Offer is already archived.",
                };
            }

            var allowed =
                string.Equals(entity.Status, CatalogOfferStatus.Draft, StringComparison.Ordinal)
                || string.Equals(entity.Status, CatalogOfferStatus.Active, StringComparison.Ordinal)
                || string.Equals(entity.Status, CatalogOfferStatus.Paused, StringComparison.Ordinal)
                || string.Equals(effective, CatalogOfferStatus.Expired, StringComparison.Ordinal);

            if (!allowed)
            {
                return new CatalogOfferLifecycleResult.InvalidStatus
                {
                    Message =
                        "Archive is only allowed from Draft, Active, Paused, or Expired offers.",
                };
            }

            entity.Status = CatalogOfferStatus.Archived;
            entity.UpdatedAt = _utcNow();
            await _context.SaveChangesAsync(cancellationToken);
            var issueCount = await CountIssuesAsync(offerId, cancellationToken);
            return new CatalogOfferLifecycleResult.Ok
            {
                Offer = ToDto(entity, utcOffsetMinutes, issueCount),
            };
        }

        public async Task<CatalogOfferLifecycleResult> DuplicateAsync(
            int offerId,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await LoadForMutationAsync(offerId, cancellationToken);
            if (entity == null)
            {
                return new CatalogOfferLifecycleResult.NotFound();
            }

            var now = _utcNow();
            var copy = new CatalogOffer
            {
                RestaurantLocationId = entity.RestaurantLocationId,
                Status = CatalogOfferStatus.Active,
                OfferType = entity.OfferType,
                Title = CatalogOfferStatus.BuildDuplicateTitle(
                    entity.Title,
                    MaxTitleLength
                ),
                Description = entity.Description,
                Validity = entity.Validity,
                CustomExpiryDate = entity.CustomExpiryDate,
                DiscountPercentage = entity.DiscountPercentage,
                DiscountAmount = entity.DiscountAmount,
                FreeItemText = entity.FreeItemText,
                PurchaseRequirement = entity.PurchaseRequirement,
                MinimumSpend = entity.MinimumSpend,
                AdditionalExclusions = entity.AdditionalExclusions,
                ReplacementItemText = entity.ReplacementItemText,
                StaffInstructions = entity.StaffInstructions,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _context.CatalogOffers.Add(copy);
            await _context.SaveChangesAsync(cancellationToken);
            return new CatalogOfferLifecycleResult.Duplicated
            {
                Offer = ToDto(copy, utcOffsetMinutes, issueCount: 0),
            };
        }

        private async Task<CatalogOffer?> LoadForMutationAsync(
            int offerId,
            CancellationToken cancellationToken
        )
        {
            return await _context.CatalogOffers
                .FirstOrDefaultAsync(offer => offer.Id == offerId, cancellationToken);
        }

        private async Task<int> CountIssuesAsync(
            int offerId,
            CancellationToken cancellationToken
        )
        {
            return await _context.OfferIssues
                .AsNoTracking()
                .CountAsync(
                    issue => issue.CatalogOfferId == offerId,
                    cancellationToken
                );
        }

        private static bool MatchesAttachSources(
            OfferProjection row,
            IReadOnlyList<string> attachSources
        )
        {
            foreach (var source in attachSources)
            {
                if (string.Equals(
                        source,
                        CatalogOfferStatus.AttachSourceCampaign,
                        StringComparison.Ordinal
                    )
                    && row.LiveAttachCount > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static CatalogOffersListItemDto ToListItem(OfferProjection row)
        {
            var entity = row.Entity;
            return new CatalogOffersListItemDto
            {
                Id = entity.Id,
                LocationId = entity.RestaurantLocationId,
                Title = entity.Title,
                Status = row.EffectiveStatus,
                OfferType = CatalogOfferMapping.ToWireOfferType(entity.OfferType),
                Validity = CatalogOfferMapping.ToWireValidity(entity.Validity),
                ExpiryDate = entity.CustomExpiryDate?.ToString("yyyy-MM-dd"),
                AttachKinds = row.AttachKinds,
                Description = entity.Description,
                LifetimeClaims = row.LifetimeClaims,
                LifetimeRedeemed = row.LifetimeRedeemed,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        private static void ValidatePaging(int page, int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentException("page must be >= 1.");
            }

            if (pageSize != DefaultPageSize)
            {
                throw new ArgumentException(
                    $"pageSize must be {DefaultPageSize}."
                );
            }
        }

        private static string NormalizeView(string? view)
        {
            var normalized = string.IsNullOrWhiteSpace(view)
                ? "all"
                : view.Trim();
            if (!AllowedViews.Contains(normalized))
            {
                throw new ArgumentException("view is invalid.");
            }

            return normalized;
        }

        private static string NormalizeSort(string? sort)
        {
            var normalized = string.IsNullOrWhiteSpace(sort)
                ? "recent-activity"
                : sort.Trim();
            if (!AllowedSorts.Contains(normalized))
            {
                throw new ArgumentException("sort is invalid.");
            }

            return normalized;
        }

        private static List<string> NormalizeStringFilters(
            IReadOnlyList<string> values,
            HashSet<string> allowed,
            string paramName
        )
        {
            var result = new List<string>();
            foreach (var raw in values)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var value = raw.Trim();
                if (!allowed.Contains(value))
                {
                    throw new ArgumentException($"{paramName} is invalid.");
                }

                if (!result.Contains(value, StringComparer.Ordinal))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        private static ParsedFields ParseAndValidateFields(
            CreateCatalogOfferRequest request
        )
        {
            if (!CatalogOfferMapping.TryParseOfferType(
                    request.OfferType,
                    out var offerType
                ))
            {
                throw new ArgumentException("Offer type is invalid.");
            }

            if (!CatalogOfferMapping.TryParseValidity(
                    request.Validity,
                    out var validity
                ))
            {
                throw new ArgumentException("Offer validity is invalid.");
            }

            var title = (request.Title ?? string.Empty).Trim();
            if (title.Length == 0 || title.Length > MaxTitleLength)
            {
                throw new ArgumentException(
                    $"Offer title is required (max {MaxTitleLength})."
                );
            }

            var description = (request.Description ?? string.Empty).Trim();
            if (description.Length == 0 || description.Length > MaxDescriptionLength)
            {
                throw new ArgumentException(
                    $"Offer description is required (max {MaxDescriptionLength})."
                );
            }

            decimal? discountPercentage = null;
            decimal? discountAmount = null;
            string? freeItemText = null;
            CatalogOfferPurchaseRequirement? purchaseRequirement = null;
            decimal? minimumSpend = null;
            string? additionalExclusions = null;
            string? replacementItemText = null;
            DateOnly? customExpiryDate = null;

            if (offerType == CatalogOfferType.PercentageDiscount)
            {
                if (request.DiscountPercentage is not { } pct || pct <= 0)
                {
                    throw new ArgumentException(
                        "Discount percentage must be greater than 0."
                    );
                }

                discountPercentage = pct;
            }
            else if (offerType == CatalogOfferType.FixedDiscount)
            {
                if (request.DiscountAmount is not { } amount || amount <= 0)
                {
                    throw new ArgumentException(
                        "Discount amount must be greater than 0."
                    );
                }

                discountAmount = amount;
            }
            else if (offerType == CatalogOfferType.FreeItem)
            {
                freeItemText = (request.FreeItemText ?? string.Empty).Trim();
                if (freeItemText.Length == 0)
                {
                    throw new ArgumentException("Free item text is required.");
                }

                if (!CatalogOfferMapping.TryParsePurchaseRequirement(
                        request.PurchaseRequirement,
                        out var req
                    ))
                {
                    throw new ArgumentException(
                        "Purchase requirement is required for free item."
                    );
                }

                purchaseRequirement = req;
                if (req == CatalogOfferPurchaseRequirement.WithMinimumSpend)
                {
                    if (request.MinimumSpend is not { } spend || spend <= 0)
                    {
                        throw new ArgumentException(
                            "Minimum spend must be greater than 0."
                        );
                    }

                    minimumSpend = spend;
                }

                additionalExclusions = string.IsNullOrWhiteSpace(
                    request.AdditionalExclusions
                )
                    ? null
                    : request.AdditionalExclusions.Trim();
            }
            else if (offerType == CatalogOfferType.ReplacementItem)
            {
                replacementItemText =
                    (request.ReplacementItemText ?? string.Empty).Trim();
                if (replacementItemText.Length == 0)
                {
                    throw new ArgumentException(
                        "Replacement item text is required."
                    );
                }
            }

            if (validity == CatalogOfferValidity.ChooseExpiryDate)
            {
                if (!DateOnly.TryParse(request.ExpiryDate, out var parsed))
                {
                    throw new ArgumentException(
                        "Expiry date is required when choosing an expiry date."
                    );
                }

                customExpiryDate = parsed;
            }

            var staffInstructions = string.IsNullOrWhiteSpace(
                request.StaffInstructions
            )
                ? null
                : request.StaffInstructions.Trim();
            if (
                staffInstructions != null
                && staffInstructions.Length > MaxStaffInstructionsLength
            )
            {
                throw new ArgumentException(
                    $"Staff instructions must be at most {MaxStaffInstructionsLength} characters."
                );
            }

            return new ParsedFields(
                offerType,
                title,
                description,
                validity,
                customExpiryDate,
                discountPercentage,
                discountAmount,
                freeItemText,
                purchaseRequirement,
                minimumSpend,
                additionalExclusions,
                replacementItemText,
                staffInstructions
            );
        }

        private CatalogOfferDto ToDto(
            CatalogOffer entity,
            int utcOffsetMinutes,
            int issueCount
        )
        {
            var today = CatalogOfferStatus.VenueLocalToday(
                _utcNow(),
                utcOffsetMinutes
            );
            var status = CatalogOfferStatus.ResolveEffectiveStatus(
                entity.Status,
                entity.Validity,
                entity.CustomExpiryDate,
                today
            );

            return new CatalogOfferDto
            {
                Id = entity.Id,
                LocationId = entity.RestaurantLocationId,
                Status = status,
                OfferType = CatalogOfferMapping.ToWireOfferType(entity.OfferType),
                Title = entity.Title,
                Description = entity.Description,
                Validity = CatalogOfferMapping.ToWireValidity(entity.Validity),
                ExpiryDate = entity.CustomExpiryDate?.ToString("yyyy-MM-dd"),
                DiscountPercentage = entity.DiscountPercentage,
                DiscountAmount = entity.DiscountAmount,
                FreeItemText = entity.FreeItemText,
                PurchaseRequirement = CatalogOfferMapping.ToWirePurchaseRequirement(
                    entity.PurchaseRequirement
                ),
                MinimumSpend = entity.MinimumSpend,
                AdditionalExclusions = entity.AdditionalExclusions,
                ReplacementItemText = entity.ReplacementItemText,
                StaffInstructions = entity.StaffInstructions,
                IssueCount = issueCount,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        private sealed record OfferProjection(
            CatalogOffer Entity,
            string EffectiveStatus,
            int LiveAttachCount,
            IReadOnlyList<string> AttachKinds,
            IReadOnlyList<string> CampaignNames,
            bool NeedsAttention,
            int LifetimeClaims,
            int LifetimeRedeemed
        );

        private sealed record ParsedFields(
            CatalogOfferType OfferType,
            string Title,
            string Description,
            CatalogOfferValidity Validity,
            DateOnly? CustomExpiryDate,
            decimal? DiscountPercentage,
            decimal? DiscountAmount,
            string? FreeItemText,
            CatalogOfferPurchaseRequirement? PurchaseRequirement,
            decimal? MinimumSpend,
            string? AdditionalExclusions,
            string? ReplacementItemText,
            string? StaffInstructions
        );
    }
}
