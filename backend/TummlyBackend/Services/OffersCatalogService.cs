using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Offers catalog create / get — Active reusable definitions (ticket 22).
    /// Not Feedback recovery-offer issue APIs.
    /// </summary>
    public class OffersCatalogService : IOffersCatalogService
    {
        public const string ActiveStatus = "active";
        public const int MaxTitleLength = 60;
        public const int MaxDescriptionLength = 240;
        public const int MaxStaffInstructionsLength = 1000;

        private readonly ApplicationDbContext _context;

        public OffersCatalogService(ApplicationDbContext context)
        {
            _context = context;
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
            var now = DateTime.UtcNow;

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
            return ToDto(entity);
        }

        public async Task<CatalogOfferDto?> GetByIdAsync(
            int offerId,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await _context.CatalogOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(offer => offer.Id == offerId, cancellationToken);

            return entity == null ? null : ToDto(entity);
        }

        public async Task<bool> IsActiveForLocationAsync(
            int offerId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            return await _context.CatalogOffers
                .AsNoTracking()
                .AnyAsync(
                    offer =>
                        offer.Id == offerId
                        && offer.RestaurantLocationId == locationId
                        && offer.Status == ActiveStatus,
                    cancellationToken
                );
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

        private static CatalogOfferDto ToDto(CatalogOffer entity)
        {
            return new CatalogOfferDto
            {
                Id = entity.Id,
                LocationId = entity.RestaurantLocationId,
                Status = entity.Status,
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
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

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
