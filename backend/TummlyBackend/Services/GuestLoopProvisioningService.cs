using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Provisioning;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Exceptions;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestLoopProvisioningService : IProvisioningService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;

        public GuestLoopProvisioningService(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
        }

        public async Task<InviteTokenResult> ValidateInviteTokenAsync(string token)
        {
            var trialRequest = await FindTrialRequestAsync(token);

            await EnsureInviteEligibleAsync(trialRequest);

            return MapInviteTokenResult(trialRequest);
        }

        public async Task ProvisionAsync(CompleteSetupDto dto)
        {
            ValidateSetupPayload(dto);

            var trialRequest = await FindTrialRequestAsync(dto.Token);

            await EnsureInviteEligibleAsync(trialRequest);

            if (
                trialRequest.AccountType == "Single"
                && dto.Locations.Count > 1
            )
            {
                throw new ArgumentException(
                    "Single-location accounts can only have one location."
                );
            }

            var fullName = string.IsNullOrWhiteSpace(dto.FullName)
                ? trialRequest.FullName?.Trim()
                : dto.FullName.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Full name is required.");
            }

            var primaryPhone = PhoneNumberHelper.NormalizeToE164(
                string.IsNullOrWhiteSpace(dto.PrimaryPhone)
                    ? trialRequest.Mobile
                    : dto.PrimaryPhone.Trim()
            );

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new User
                {
                    FullName = fullName,
                    Email = trialRequest.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    PhoneNumber = primaryPhone,
                    Role = "Owner",
                    AccountType = trialRequest.AccountType,
                    IsEmailVerified = true,
                    IsApprovedByAdmin = true,
                    IsLocked = false,
                    FailedLoginAttempts = 0
                };

                _context.Users.Add(user);

                trialRequest.IsAccountCreated = true;
                trialRequest.Status = "Account Created";

                await _context.SaveChangesAsync();

                var restaurant = new Restaurant
                {
                    Name = dto.GroupName,
                    AccountType = trialRequest.AccountType,
                    OwnerUserId = user.Id,
                    BusinessCategory = dto.BusinessCategory,
                    BusinessLink = dto.BusinessLink,
                    PublicPhoneNumber = primaryPhone,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Restaurants.Add(restaurant);
                await _context.SaveChangesAsync();

                foreach (var item in dto.Locations)
                {
                    var location = new RestaurantLocation
                    {
                        RestaurantId = restaurant.Id,
                        LinkToken = await _smartGuestLink.GenerateTokenAsync(),
                        LocationName = item.LocationName ?? "",
                        Address = item.Address ?? "",
                        Postcode = string.IsNullOrWhiteSpace(item.Postcode)
                            ? null
                            : UkPostcode.FormatForDisplay(item.Postcode),
                        LocationPhone = PhoneNumberHelper.NormalizeOptional(
                            item.LocationPhone
                        ),
                        LocalContact = item.LocalContact,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.RestaurantLocations.Add(location);
                }

                await _context.SaveChangesAsync();

                var guestLoop = new GuestLoopSetup
                {
                    RestaurantId = restaurant.Id,
                    SendPhysicalQrMaterials = false,
                    AutoSendReviewRequests = true,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.GuestLoopSetups.Add(guestLoop);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static void ValidateSetupPayload(CompleteSetupDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
            {
                throw new ArgumentException("Passwords do not match.");
            }

            if (dto.Password.Length < 8)
            {
                throw new ArgumentException(
                    "Password must be at least 8 characters."
                );
            }

            if (string.IsNullOrWhiteSpace(dto.GroupName))
            {
                throw new ArgumentException("Group name is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.BusinessCategory))
            {
                throw new ArgumentException("Business category is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.PrimaryPhone))
            {
                throw new ArgumentException("Primary phone is required.");
            }

            if (dto.Locations == null || !dto.Locations.Any())
            {
                throw new ArgumentException("At least one location is required.");
            }
        }

        private async Task<TrialRequest> FindTrialRequestAsync(string? token)
        {
            var normalizedToken = token?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                throw new InviteTokenNotFoundException();
            }

            var trialRequest =
                await _context.TrialRequests.FirstOrDefaultAsync(x =>
                    x.ApprovalToken != null
                    && x.ApprovalToken.Trim() == normalizedToken
                );

            if (trialRequest == null)
            {
                throw new InviteTokenNotFoundException();
            }

            return trialRequest;
        }

        private async Task EnsureInviteEligibleAsync(TrialRequest trialRequest)
        {
            if (!trialRequest.IsApproved)
            {
                throw new InviteTokenNotApprovedException();
            }

            if (
                trialRequest.InviteExpiresAt.HasValue
                && trialRequest.InviteExpiresAt.Value < DateTime.UtcNow
            )
            {
                throw new InviteTokenExpiredException();
            }

            if (trialRequest.IsAccountCreated)
            {
                throw new AccountAlreadyCreatedException();
            }

            var existingUser =
                await _context.Users.FirstOrDefaultAsync(x =>
                    x.Email == trialRequest.Email
                );

            if (existingUser != null)
            {
                throw new AccountAlreadyCreatedException(
                    "User already exists."
                );
            }
        }

        private static InviteTokenResult MapInviteTokenResult(
            TrialRequest trialRequest
        )
        {
            return new InviteTokenResult
            {
                AccountType = trialRequest.AccountType,
                Email = trialRequest.Email,
                FullName = trialRequest.FullName,
                RestaurantName = trialRequest.BusinessName,
                GroupName = trialRequest.BusinessName,
                Mobile = trialRequest.Mobile,
                BusinessCategory = trialRequest.BusinessCategory,
                Locations =
                    trialRequest.AccountType == "Multi"
                        ? trialRequest.Locations
                        : null,
                Role = trialRequest.Role,
                TrialRequestId = trialRequest.Id,
                ExpiresAt = trialRequest.InviteExpiresAt
            };
        }

    }
}
