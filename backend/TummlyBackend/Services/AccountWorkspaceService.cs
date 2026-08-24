using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.AccountWorkspace;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class AccountWorkspaceService : IAccountWorkspaceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IQueryAttachmentStorage _attachmentStorage;

        public AccountWorkspaceService(
            ApplicationDbContext context,
            IQueryAttachmentStorage attachmentStorage
        )
        {
            _context = context;
            _attachmentStorage = attachmentStorage;
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

            return await BuildDetailsAsync(restaurant);
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

            var details = await BuildDetailsAsync(restaurant);
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
            Restaurant restaurant
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
                MainOperatingCountry = "United Kingdom",
                BrandLogoOperatorUrl = hasLogo
                    ? BrandLogoRules.OperatorBrandLogoUrl
                    : null,
                BrandLogoPublicUrl = hasLogo
                    ? BrandLogoRules.BuildPublicUrl(restaurant.BrandLogoObjectKey!)
                    : null,
                LastSavedAt = restaurant.AccountWorkspaceLastSavedAt,
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
                BusinessDetails = null,
                KeyContacts = null,
                WorkspaceDefaults = null,
            };
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
