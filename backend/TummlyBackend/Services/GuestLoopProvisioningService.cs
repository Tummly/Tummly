using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IQrCodeProvisioningService _qrCodeProvisioning;
        private readonly IConfiguration _configuration;

        public GuestLoopProvisioningService(
            ApplicationDbContext context,
            IQrCodeProvisioningService qrCodeProvisioning,
            IConfiguration configuration
        )
        {
            _context = context;
            _qrCodeProvisioning = qrCodeProvisioning;
            _configuration = configuration;
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

            var primaryPhone = PhoneNumberHelper.NormalizeOptional(
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
                    PhoneNumber = primaryPhone ?? string.Empty,
                    Role = "Owner",
                    AccountType = trialRequest.AccountType,
                    IsEmailVerified = true,
                    IsApprovedByAdmin = true,
                    IsLocked = false,
                    FailedLoginAttempts = 0,
                    TermsAccepted = trialRequest.TermsAccepted,
                };

                _context.Users.Add(user);

                trialRequest.IsAccountCreated = true;
                trialRequest.Status = TrialRequestStatus.AccountCreated;

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

                    await _qrCodeProvisioning.MintDefaultQrCodesAsync(location);
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

        public async Task GenerateActivationCodeAsync(string inviteToken)
        {
            var trialRequest = await FindTrialRequestAsync(inviteToken);

            if (!trialRequest.IsAccountCreated)
            {
                throw new ArgumentException(
                    "Account setup must complete before generating an activation code."
                );
            }

            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Email == trialRequest.Email
            );

            if (user == null)
            {
                throw new ArgumentException(
                    "Operator account was not found for this invite."
                );
            }

            if (!string.IsNullOrEmpty(user.ActivationCodeHash))
            {
                return;
            }

            var plainCode = ActivationCodeHelper.GeneratePlainCode();
            user.ActivationCodeHash =
                ActivationCodeHelper.HashCode(plainCode);
            user.ActivationCodeEncrypted =
                ActivationCodeProtectionHelper.Encrypt(
                    plainCode,
                    GetActivationProtectionKey()
                );

            await _context.SaveChangesAsync();
        }

        private string GetActivationProtectionKey()
        {
            var secret = _configuration["JwtSettings:Secret"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    "JwtSettings:Secret is required for activation code protection."
                );
            }

            return secret;
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
