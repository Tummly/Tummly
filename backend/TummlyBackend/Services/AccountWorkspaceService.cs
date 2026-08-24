using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TummlyBackend.Data;
using TummlyBackend.DTOs.AccountWorkspace;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class AccountWorkspaceService : IAccountWorkspaceService
    {
        private const string DefaultCountry = "United Kingdom";

        private readonly ApplicationDbContext _context;
        private readonly IQueryAttachmentStorage _attachmentStorage;
        private readonly IDistributedCache _cache;

        public AccountWorkspaceService(
            ApplicationDbContext context,
            IQueryAttachmentStorage attachmentStorage,
            IDistributedCache cache
        )
        {
            _context = context;
            _attachmentStorage = attachmentStorage;
            _cache = cache;
        }

        public async Task<AccountWorkspaceDetailsDto?> GetDetailsAsync(
            int ownerUserId
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerUserId == ownerUserId);

            if (restaurant == null)
            {
                return null;
            }

            return await BuildDetailsAsync(restaurant, ownerUserId);
        }

        public async Task<(
            AccountWorkspaceDetailsDto? Details,
            string? Error,
            int StatusCode
        )> UpdateAccountDetailsAsync(
            int ownerUserId,
            string? name,
            IFormFile? logo
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == ownerUserId);

            if (restaurant == null)
            {
                return (null, "Restaurant not found.", StatusCodes.Status404NotFound);
            }

            var trimmedName = name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return (
                    null,
                    "Workspace name is required.",
                    StatusCodes.Status400BadRequest
                );
            }

            if (trimmedName.Length > 200)
            {
                return (
                    null,
                    "Workspace name must be 200 characters or fewer.",
                    StatusCodes.Status400BadRequest
                );
            }

            var hasLogo = logo != null && logo.Length > 0;

            if (hasLogo)
            {
                var logoError = BrandLogoRules.ValidateFile(logo);

                if (logoError != null)
                {
                    return (
                        null,
                        logoError,
                        StatusCodes.Status400BadRequest
                    );
                }

                if (!_attachmentStorage.IsConfigured)
                {
                    return (
                        null,
                        "Object storage is not configured.",
                        StatusCodes.Status503ServiceUnavailable
                    );
                }
            }

            string? uploadedKey = null;
            string? uploadedContentType = null;

            if (hasLogo)
            {
                uploadedContentType = BrandLogoRules.ResolveContentType(
                    logo!.ContentType,
                    logo.FileName
                )!;
                uploadedKey = BrandLogoRules.BuildStorageKey(logo.FileName);

                await using var stream = logo.OpenReadStream();
                await _attachmentStorage.UploadAsync(
                    uploadedKey,
                    stream,
                    uploadedContentType,
                    logo.Length
                );
            }

            restaurant.Name = trimmedName;

            if (uploadedKey != null)
            {
                var previousKey = restaurant.BrandLogoObjectKey;
                restaurant.BrandLogoObjectKey = uploadedKey;
                restaurant.BrandLogoContentType = uploadedContentType;

                if (
                    !string.IsNullOrWhiteSpace(previousKey)
                    && !string.Equals(
                        previousKey,
                        uploadedKey,
                        StringComparison.Ordinal
                    )
                )
                {
                    try
                    {
                        await _attachmentStorage.DeleteAsync(previousKey);
                    }
                    catch
                    {
                        // Best-effort cleanup of the previous mark.
                    }
                }
            }

            restaurant.AccountWorkspaceLastSavedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var details = await BuildDetailsAsync(restaurant, ownerUserId);
            return (details, null, StatusCodes.Status200OK);
        }

        public async Task<(
            AccountWorkspaceDetailsDto? Details,
            string? Error,
            int StatusCode
        )> UpdateBusinessDetailsAsync(
            int ownerUserId,
            UpdateBusinessDetailsRequest request
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == ownerUserId);

            if (restaurant == null)
            {
                return (null, "Restaurant not found.", StatusCodes.Status404NotFound);
            }

            var validationError = ValidateBusinessDetails(request);
            if (validationError != null)
            {
                return (
                    null,
                    validationError,
                    StatusCodes.Status400BadRequest
                );
            }

            var row = await _context.RestaurantBusinessDetails
                .FirstOrDefaultAsync(d => d.RestaurantId == restaurant.Id);

            if (row == null)
            {
                row = new RestaurantBusinessDetails
                {
                    RestaurantId = restaurant.Id,
                };
                _context.RestaurantBusinessDetails.Add(row);
            }

            var legalName = TrimToNull(request.LegalBusinessName, 200);
            var tradingName = TrimToNull(request.TradingName, 200);

            if (request.SameAsLegalBusinessName == true)
            {
                tradingName = legalName;
            }

            row.LegalStructure = LegalStructureOptions.Normalize(
                request.LegalStructure
            );
            row.LegalBusinessName = legalName;
            row.TradingName = tradingName;
            row.CompanyNumber = TrimToNull(request.CompanyNumber, 50);
            row.VatNumber = TrimToNull(request.VatNumber, 50);
            row.CountryOfRegistration = TrimToNull(
                request.CountryOfRegistration,
                100
            );
            row.AddressLine1 = TrimToNull(request.AddressLine1, 500);
            row.AddressLine2 = TrimToNull(request.AddressLine2, 500);
            row.TownCity = TrimToNull(request.TownCity, 150);
            row.County = TrimToNull(request.County, 150);
            row.Postcode = TrimToNull(request.Postcode, 20);
            row.Country = TrimToNull(request.Country, 100);

            restaurant.AccountWorkspaceLastSavedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var details = await BuildDetailsAsync(restaurant, ownerUserId);
            return (details, null, StatusCodes.Status200OK);
        }

        public async Task<(
            AccountWorkspaceDetailsDto? Details,
            string? Error,
            int StatusCode
        )> UpdateKeyContactsAsync(
            int ownerUserId,
            UpdateKeyContactsRequest request
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == ownerUserId);

            if (restaurant == null)
            {
                return (null, "Restaurant not found.", StatusCodes.Status404NotFound);
            }

            EnsureContactDefaults(restaurant);

            if (
                request.AccountOwnerUserId is int claimedOwner
                && claimedOwner != restaurant.OwnerUserId
            )
            {
                return (
                    null,
                    "Account owner cannot be changed on this tab.",
                    StatusCodes.Status400BadRequest
                );
            }

            if (
                request.BillingContactUserId <= 0
                || request.PrivacyContactUserId <= 0
                || request.SupportContactUserId <= 0
            )
            {
                return (
                    null,
                    "Billing, privacy, and support contacts are required.",
                    StatusCodes.Status400BadRequest
                );
            }

            var eligibleIds = GetEligibleMemberIds(restaurant);
            if (
                !eligibleIds.Contains(request.BillingContactUserId)
                || !eligibleIds.Contains(request.PrivacyContactUserId)
                || !eligibleIds.Contains(request.SupportContactUserId)
            )
            {
                return (
                    null,
                    "Each contact must be an eligible team member.",
                    StatusCodes.Status400BadRequest
                );
            }

            restaurant.BillingContactUserId = request.BillingContactUserId;
            restaurant.PrivacyContactUserId = request.PrivacyContactUserId;
            restaurant.SupportContactUserId = request.SupportContactUserId;
            restaurant.AccountWorkspaceLastSavedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var details = await BuildDetailsAsync(restaurant, ownerUserId);
            return (details, null, StatusCodes.Status200OK);
        }

        public async Task<(
            AccountWorkspaceDetailsDto? Details,
            string? Error,
            int StatusCode
        )> UpdateWorkspaceDefaultsAsync(
            int ownerUserId,
            UpdateWorkspaceDefaultsRequest request
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == ownerUserId);

            if (restaurant == null)
            {
                return (null, "Restaurant not found.", StatusCodes.Status404NotFound);
            }

            var previousPeriod = WorkspaceDefaultsOptions.NormalizeReportingPeriod(
                restaurant.DefaultReportingPeriod
            );

            restaurant.WeekStartsOn = WorkspaceDefaultsOptions.NormalizeWeekStartsOn(
                request.WeekStartsOn
            );
            restaurant.DefaultReportingPeriod =
                WorkspaceDefaultsOptions.NormalizeReportingPeriod(
                    request.DefaultReportingPeriod
                );
            restaurant.DefaultCampaignSenderName =
                WorkspaceDefaultsOptions.NormalizeCampaignSenderName(
                    request.DefaultCampaignSenderName
                );
            restaurant.AccountWorkspaceLastSavedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (
                !string.Equals(
                    previousPeriod,
                    restaurant.DefaultReportingPeriod,
                    StringComparison.Ordinal
                )
            )
            {
                await BustRecommendedNextStepCachesAsync(
                    ownerUserId,
                    restaurant.Id
                );
            }

            var details = await BuildDetailsAsync(restaurant, ownerUserId);
            return (details, null, StatusCodes.Status200OK);
        }

        public async Task<(
            AccountWorkspaceDetailsDto? Details,
            string? Error,
            int StatusCode
        )> PauseWorkspaceAsync(int actorUserId)
        {
            return await SetWorkspaceStatusAsync(
                actorUserId,
                WorkspaceStatus.Paused
            );
        }

        public async Task<(
            AccountWorkspaceDetailsDto? Details,
            string? Error,
            int StatusCode
        )> ResumeWorkspaceAsync(int actorUserId)
        {
            return await SetWorkspaceStatusAsync(
                actorUserId,
                WorkspaceStatus.Active
            );
        }

        private async Task<(
            AccountWorkspaceDetailsDto? Details,
            string? Error,
            int StatusCode
        )> SetWorkspaceStatusAsync(
            int actorUserId,
            WorkspaceStatus nextStatus
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == actorUserId);

            if (restaurant == null)
            {
                return (null, "Restaurant not found.", StatusCodes.Status404NotFound);
            }

            restaurant.WorkspaceStatus = nextStatus;
            restaurant.WorkspaceStatusChangedAt = DateTime.UtcNow;
            restaurant.WorkspaceStatusChangedByUserId = actorUserId;
            await _context.SaveChangesAsync();

            var details = await BuildDetailsAsync(restaurant, actorUserId);
            return (details, null, StatusCodes.Status200OK);
        }

        public async Task<(Stream Stream, string ContentType)?> OpenBrandLogoAsync(
            int ownerUserId
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerUserId == ownerUserId);

            if (
                restaurant == null
                || string.IsNullOrWhiteSpace(restaurant.BrandLogoObjectKey)
            )
            {
                return null;
            }

            if (!_attachmentStorage.IsConfigured)
            {
                return null;
            }

            var stream = await _attachmentStorage.OpenReadAsync(
                restaurant.BrandLogoObjectKey
            );
            var contentType =
                restaurant.BrandLogoContentType ?? "application/octet-stream";
            return (stream, contentType);
        }

        public async Task<(Stream Stream, string ContentType)?>
            OpenPublicBrandLogoAsync(string objectKey)
        {
            if (
                string.IsNullOrWhiteSpace(objectKey)
                || !objectKey.StartsWith(
                    BrandLogoRules.PublicObjectPrefix,
                    StringComparison.Ordinal
                )
            )
            {
                return null;
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.BrandLogoObjectKey == objectKey);

            if (restaurant == null)
            {
                return null;
            }

            if (!_attachmentStorage.IsConfigured)
            {
                return null;
            }

            var stream = await _attachmentStorage.OpenReadAsync(objectKey);
            var contentType =
                restaurant.BrandLogoContentType ?? "application/octet-stream";
            return (stream, contentType);
        }

        private async Task<AccountWorkspaceDetailsDto> BuildDetailsAsync(
            Restaurant restaurant,
            int actorUserId
        )
        {
            var activeLocations = await _context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(l =>
                    l.RestaurantId == restaurant.Id
                    && l.CaptureLocationStatus == CaptureLocationStatus.Active
                );

            var guestProfiles = await _context.MasterGuests
                .AsNoTracking()
                .CountAsync(g => g.RestaurantId == restaurant.Id);

            var businessDetails = await _context.RestaurantBusinessDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.RestaurantId == restaurant.Id);

            EnsureContactDefaults(restaurant);

            var owner = await _context.Users
                .AsNoTracking()
                .FirstAsync(u => u.Id == restaurant.OwnerUserId);

            var keyContacts = MapKeyContacts(restaurant, owner);

            var workspaceStatus = restaurant.WorkspaceStatus.ToString();
            var lastAccountUpdate =
                restaurant.AccountWorkspaceLastSavedAt ?? restaurant.CreatedAt;

            var hasLogo = !string.IsNullOrWhiteSpace(restaurant.BrandLogoObjectKey);

            return new AccountWorkspaceDetailsDto
            {
                Success = true,
                WorkspaceName = restaurant.Name,
                AccountStructure = ResolveAccountStructure(restaurant.AccountType),
                BusinessCategory = restaurant.BusinessCategory,
                BusinessCategoryLabel = BusinessCategoryLabels.ResolveLabel(
                    restaurant.BusinessCategory
                ),
                MainOperatingCountry = DefaultCountry,
                BrandLogoOperatorUrl = hasLogo
                    ? BrandLogoRules.OperatorBrandLogoUrl
                    : null,
                BrandLogoPublicUrl = hasLogo
                    ? BrandLogoRules.BuildPublicUrl(restaurant.BrandLogoObjectKey!)
                    : null,
                LastSavedAt = restaurant.AccountWorkspaceLastSavedAt,
                IsAccountOwner = restaurant.OwnerUserId == actorUserId,
                RestaurantId = restaurant.Id,
                Status = new AccountWorkspaceStatusDto
                {
                    WorkspaceStatus = workspaceStatus,
                    PlanStatus = "Pilot",
                    BillingStatus = "Active",
                    AccountCreatedAt = restaurant.CreatedAt,
                    ActiveLocations = activeLocations,
                    TeamMembers = 1,
                    GuestProfiles = guestProfiles,
                    GuestFormStatus =
                        restaurant.WorkspaceStatus == WorkspaceStatus.Active
                            ? "Live"
                            : "Paused",
                    LastAccountUpdateAt = lastAccountUpdate,
                },
                BusinessDetails = MapBusinessDetails(businessDetails),
                KeyContacts = keyContacts,
                WorkspaceDefaults = MapWorkspaceDefaults(restaurant),
            };
        }

        private async Task BustRecommendedNextStepCachesAsync(
            int ownerUserId,
            int restaurantId
        )
        {
            var locationIds = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.RestaurantId == restaurantId)
                .Select(l => l.Id)
                .ToListAsync();

            foreach (var locationId in locationIds)
            {
                foreach (var period in WorkspaceDefaultsOptions.ReportingPeriodValues)
                {
                    await _cache.RemoveAsync(
                        HomeRecommendationContract.BuildCacheKey(
                            ownerUserId,
                            locationId,
                            period,
                            fromUtc: null,
                            toUtc: null
                        )
                    );
                    await _cache.RemoveAsync(
                        $"campaign-recommendation:{ownerUserId}:{locationId}:{period}"
                    );
                }

                var offerIds = await _context.CatalogOffers
                    .AsNoTracking()
                    .Where(offer => offer.RestaurantLocationId == locationId)
                    .Select(offer => offer.Id)
                    .ToListAsync();

                foreach (var offerId in offerIds)
                {
                    foreach (var period in WorkspaceDefaultsOptions.ReportingPeriodValues)
                    {
                        await _cache.RemoveAsync(
                            OfferRecommendationContract.BuildCacheKey(
                                ownerUserId,
                                locationId,
                                offerId,
                                period
                            )
                        );
                    }
                }
            }
        }

        private static AccountWorkspaceWorkspaceDefaultsDto MapWorkspaceDefaults(
            Restaurant restaurant
        )
        {
            return new AccountWorkspaceWorkspaceDefaultsDto
            {
                WeekStartsOn = WorkspaceDefaultsOptions.NormalizeWeekStartsOn(
                    restaurant.WeekStartsOn
                ),
                DefaultReportingPeriod =
                    WorkspaceDefaultsOptions.NormalizeReportingPeriod(
                        restaurant.DefaultReportingPeriod
                    ),
                DefaultCampaignSenderName = restaurant.DefaultCampaignSenderName,
                DefaultTimezone = WorkspaceDefaultsOptions.DefaultTimezone,
                DefaultCurrency = WorkspaceDefaultsOptions.DefaultCurrency,
                DefaultLanguage = WorkspaceDefaultsOptions.DefaultLanguage,
                DateFormat = WorkspaceDefaultsOptions.DefaultDateFormat,
            };
        }

        private static void EnsureContactDefaults(Restaurant restaurant)
        {
            if (restaurant.BillingContactUserId == 0)
            {
                restaurant.BillingContactUserId = restaurant.OwnerUserId;
            }

            if (restaurant.PrivacyContactUserId == 0)
            {
                restaurant.PrivacyContactUserId = restaurant.OwnerUserId;
            }

            if (restaurant.SupportContactUserId == 0)
            {
                restaurant.SupportContactUserId = restaurant.OwnerUserId;
            }
        }

        private static HashSet<int> GetEligibleMemberIds(Restaurant restaurant)
        {
            // Until Team & permissions ships, directory is { Account owner }.
            return [restaurant.OwnerUserId];
        }

        private static AccountWorkspaceKeyContactsDto MapKeyContacts(
            Restaurant restaurant,
            User owner
        )
        {
            var ownerItem = MapPickerItem(owner);
            return new AccountWorkspaceKeyContactsDto
            {
                AccountOwner = ownerItem,
                BillingContactUserId = restaurant.BillingContactUserId,
                PrivacyContactUserId = restaurant.PrivacyContactUserId,
                SupportContactUserId = restaurant.SupportContactUserId,
                EligibleMembers = [ownerItem],
            };
        }

        private static TeamMemberPickerItemDto MapPickerItem(User user)
        {
            return new TeamMemberPickerItemDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
            };
        }

        private static RestaurantBusinessDetailsDto MapBusinessDetails(
            RestaurantBusinessDetails? row
        )
        {
            if (row == null)
            {
                return new RestaurantBusinessDetailsDto
                {
                    CountryOfRegistration = DefaultCountry,
                    Country = DefaultCountry,
                };
            }

            return new RestaurantBusinessDetailsDto
            {
                LegalStructure = row.LegalStructure,
                LegalBusinessName = row.LegalBusinessName,
                TradingName = row.TradingName,
                CompanyNumber = row.CompanyNumber,
                VatNumber = row.VatNumber,
                CountryOfRegistration =
                    string.IsNullOrWhiteSpace(row.CountryOfRegistration)
                        ? DefaultCountry
                        : row.CountryOfRegistration,
                AddressLine1 = row.AddressLine1,
                AddressLine2 = row.AddressLine2,
                TownCity = row.TownCity,
                County = row.County,
                Postcode = row.Postcode,
                Country =
                    string.IsNullOrWhiteSpace(row.Country)
                        ? DefaultCountry
                        : row.Country,
            };
        }

        private static string? ValidateBusinessDetails(
            UpdateBusinessDetailsRequest request
        )
        {
            if (!LegalStructureOptions.IsValid(request.LegalStructure))
            {
                return "Legal structure is not a recognised option.";
            }

            if (Exceeds(request.LegalBusinessName, 200))
            {
                return "Legal business name must be 200 characters or fewer.";
            }

            if (Exceeds(request.TradingName, 200))
            {
                return "Trading name must be 200 characters or fewer.";
            }

            if (Exceeds(request.CompanyNumber, 50))
            {
                return "Company number must be 50 characters or fewer.";
            }

            if (Exceeds(request.VatNumber, 50))
            {
                return "VAT number must be 50 characters or fewer.";
            }

            if (Exceeds(request.CountryOfRegistration, 100))
            {
                return "Country of registration must be 100 characters or fewer.";
            }

            if (Exceeds(request.AddressLine1, 500))
            {
                return "Address line 1 must be 500 characters or fewer.";
            }

            if (Exceeds(request.AddressLine2, 500))
            {
                return "Address line 2 must be 500 characters or fewer.";
            }

            if (Exceeds(request.TownCity, 150))
            {
                return "Town or city must be 150 characters or fewer.";
            }

            if (Exceeds(request.County, 150))
            {
                return "County must be 150 characters or fewer.";
            }

            if (Exceeds(request.Postcode, 20))
            {
                return "Postcode must be 20 characters or fewer.";
            }

            if (Exceeds(request.Country, 100))
            {
                return "Country must be 100 characters or fewer.";
            }

            var country = request.Country?.Trim() ?? string.Empty;
            var postcode = request.Postcode?.Trim() ?? string.Empty;
            var isUnitedKingdom = string.Equals(
                country,
                DefaultCountry,
                StringComparison.OrdinalIgnoreCase
            );

            if (
                isUnitedKingdom
                && postcode.Length > 0
                && !UkPostcode.IsValidFormat(postcode)
            )
            {
                return "Enter a valid UK postcode.";
            }

            return null;
        }

        private static bool Exceeds(string? value, int max)
        {
            return value != null && value.Trim().Length > max;
        }

        private static string? TrimToNull(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length > max ? trimmed[..max] : trimmed;
        }

        private static string ResolveAccountStructure(string accountType)
        {
            return string.Equals(
                accountType,
                "Multi",
                StringComparison.OrdinalIgnoreCase
            )
                ? "Multi-location"
                : "Single location";
        }
    }
}
