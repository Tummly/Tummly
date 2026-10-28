using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Provisioning;
using TummlyBackend.DTOs.Signup;
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
        private readonly IPrintReadyQrMaterialsWork _printReadyQrMaterialsWork;
        private readonly IConfiguration _configuration;
        private readonly IPricebookCatalog _pricebookCatalog;
        private readonly IBillingAccountLifecycle _lifecycle;
        private readonly IFirstPaidConversionPaySession _firstPaidConversionPaySession;
        private readonly CustomPlanCycleBillingService _customPlanCycleBilling;
        private readonly IPilotStartService _pilotStart;
        private readonly ILogger<GuestLoopProvisioningService> _logger;

        public GuestLoopProvisioningService(
            ApplicationDbContext context,
            IQrCodeProvisioningService qrCodeProvisioning,
            IPrintReadyQrMaterialsWork printReadyQrMaterialsWork,
            IConfiguration configuration,
            IPricebookCatalog pricebookCatalog,
            IBillingAccountLifecycle lifecycle,
            IFirstPaidConversionPaySession firstPaidConversionPaySession,
            CustomPlanCycleBillingService customPlanCycleBilling,
            IPilotStartService pilotStart,
            ILogger<GuestLoopProvisioningService> logger
        )
        {
            _logger = logger;
            _context = context;
            _qrCodeProvisioning = qrCodeProvisioning;
            _printReadyQrMaterialsWork = printReadyQrMaterialsWork;
            _configuration = configuration;
            _pricebookCatalog = pricebookCatalog;
            _lifecycle = lifecycle;
            _firstPaidConversionPaySession = firstPaidConversionPaySession;
            _customPlanCycleBilling = customPlanCycleBilling;
            _pilotStart = pilotStart;
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

            var locations = dto.Locations
                .Select(item => new ProvisionLocationInput(
                    item.LocationName,
                    item.Address,
                    item.City,
                    item.Postcode,
                    item.LocationPhone,
                    item.LocalContact
                ))
                .ToList();

            await CreateOperatorAccountAsync(
                new ProvisionAccountInput(
                    Email: trialRequest.Email,
                    PasswordHash: BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    FullName: fullName,
                    AccountType: trialRequest.AccountType,
                    TermsAccepted: trialRequest.TermsAccepted,
                    GroupName: dto.GroupName,
                    BusinessCategory: dto.BusinessCategory,
                    PrimaryPhone: primaryPhone,
                    BusinessLink: dto.BusinessLink,
                    Locations: locations
                ),
                afterUserSaved: () =>
                {
                    trialRequest.IsAccountCreated = true;
                    trialRequest.Status = TrialRequestStatus.AccountCreated;
                },
                startsPilot: true
            );
        }

        public async Task RetryPaidSignupCheckoutAsync(Guid pendingSignupId)
        {
            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Id == pendingSignupId
            );
            if (
                pending == null
                || pending.Status != PendingSignupStatuses.Complete
                || string.IsNullOrWhiteSpace(pending.ChosenPlan)
                || pending.ChosenPlan
                    is BillingSubscriptionPlans.Free
                        or BillingSubscriptionPlans.Pilot
            )
            {
                return;
            }

            await StartPaidSignupPaySessionAsync(
                pending.Email,
                pending.ChosenPlan,
                pending.ChosenCadence ?? "monthly",
                pending.Id,
                pending.CustomPlanConfigJson
            );
        }

        public async Task ProvisionFromPendingAsync(Guid pendingSignupId)
        {
            var pending = await _context.PendingSignups.FirstOrDefaultAsync(x =>
                x.Id == pendingSignupId
            );

            if (pending == null)
            {
                throw new ArgumentException("No pending signup found.");
            }

            if (pending.Status == PendingSignupStatuses.Complete)
            {
                return;
            }

            var isSocial = !string.IsNullOrWhiteSpace(pending.AuthProvider);

            if (string.IsNullOrWhiteSpace(pending.PasswordHash) && !isSocial)
            {
                throw new ArgumentException(
                    "Pending signup is missing a password hash."
                );
            }

            if (isSocial && string.IsNullOrWhiteSpace(pending.ProviderSubject))
            {
                throw new ArgumentException(
                    "Pending signup is missing a provider subject."
                );
            }

            if (string.IsNullOrWhiteSpace(pending.OnboardingJson))
            {
                throw new ArgumentException(
                    "Pending signup is missing onboarding data."
                );
            }

            var profile = JsonSerializer.Deserialize<SignupOnboardingPayload>(
                pending.OnboardingJson
            );

            if (profile == null || profile.Locations.Count == 0)
            {
                throw new ArgumentException(
                    "Pending signup onboarding data is invalid."
                );
            }

            var accountType = string.IsNullOrWhiteSpace(pending.AccountType)
                ? (profile.Locations.Count == 1 ? "Single" : "Multi")
                : pending.AccountType;

            if (accountType == "Single" && profile.Locations.Count > 1)
            {
                throw new ArgumentException(
                    "Single-location accounts can only have one location."
                );
            }

            var fullName = string.IsNullOrWhiteSpace(pending.FullName)
                ? profile.FullName?.Trim()
                : pending.FullName.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(profile.GroupName))
            {
                throw new ArgumentException("Group name is required.");
            }

            if (string.IsNullOrWhiteSpace(profile.BusinessCategory))
            {
                throw new ArgumentException("Business category is required.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(x =>
                x.Email == pending.Email
            );

            if (existingUser != null)
            {
                // Race / retry: user already exists — ensure social link if missing,
                // mark complete and exit.
                if (
                    isSocial
                    && !string.IsNullOrWhiteSpace(pending.ProviderSubject)
                )
                {
                    var hasLink = await _context.UserExternalLogins.AnyAsync(x =>
                        x.UserId == existingUser.Id
                        && x.Provider == pending.AuthProvider
                        && x.ProviderSubject == pending.ProviderSubject
                    );

                    if (!hasLink)
                    {
                        _context.UserExternalLogins.Add(
                            new UserExternalLogin
                            {
                                UserId = existingUser.Id,
                                Provider = pending.AuthProvider!,
                                ProviderSubject = pending.ProviderSubject!,
                                CreatedAtUtc = DateTime.UtcNow,
                            }
                        );
                    }
                }

                pending.Status = PendingSignupStatuses.Complete;
                pending.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return;
            }

            var primaryPhone = PhoneNumberHelper.NormalizeOptional(
                profile.PrimaryPhone
            );

            var locations = profile.Locations
                .Select(item => new ProvisionLocationInput(
                    item.LocationName,
                    item.Address,
                    item.City,
                    item.Postcode,
                    item.LocationPhone,
                    item.LocalContact
                ))
                .ToList();

            // Free uses CreateDefaultBillingAccount defaults.
            // Pilot / paid plans apply after create from ChosenPlan.
            var chosenPlan = pending.ChosenPlan?.Trim() ?? BillingSubscriptionPlans.Free;
            var isPilotPlan = string.Equals(
                chosenPlan,
                BillingSubscriptionPlans.Pilot,
                StringComparison.OrdinalIgnoreCase
            );
            var isPaidPlan =
                !string.IsNullOrWhiteSpace(pending.ChosenPlan)
                && !isPilotPlan
                && !string.Equals(
                    chosenPlan,
                    BillingSubscriptionPlans.Free,
                    StringComparison.OrdinalIgnoreCase
                );

            await CreateOperatorAccountAsync(
                new ProvisionAccountInput(
                    Email: pending.Email,
                    PasswordHash: pending.PasswordHash,
                    FullName: fullName,
                    AccountType: accountType,
                    TermsAccepted: pending.TermsAccepted,
                    GroupName: profile.GroupName.Trim(),
                    BusinessCategory: profile.BusinessCategory.Trim(),
                    PrimaryPhone: primaryPhone,
                    BusinessLink: profile.BusinessLink,
                    Locations: locations,
                    AuthProvider: pending.AuthProvider,
                    ProviderSubject: pending.ProviderSubject
                ),
                afterUserSaved: () =>
                {
                    pending.Status = PendingSignupStatuses.Complete;
                    pending.UpdatedAtUtc = DateTime.UtcNow;
                },
                startsPilot: false
            );

            if (isPilotPlan)
            {
                await ApplyPilotSignupBillingAsync(pending.Email);
            }
            else if (isPaidPlan)
            {
                // Stay Free until Revolut webhook; open pay session for checkout.
                await StartPaidSignupPaySessionAsync(
                    pending.Email,
                    pending.ChosenPlan!,
                    pending.ChosenCadence ?? "monthly",
                    pending.Id,
                    pending.CustomPlanConfigJson
                );
            }
        }

        public async Task EnsurePilotForPendingAsync(Guid pendingSignupId)
        {
            var pending = await _context.PendingSignups
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == pendingSignupId);
            if (
                pending == null
                || pending.Status != PendingSignupStatuses.Complete
                || !string.Equals(
                    pending.ChosenPlan?.Trim(),
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return;
            }

            await ApplyPilotSignupBillingAsync(pending.Email);
        }

        private async Task ApplyPilotSignupBillingAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Email == email
            );
            if (user == null)
            {
                return;
            }

            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(x =>
                x.OwnerUserId == user.Id
            );
            if (restaurant == null)
            {
                return;
            }

            var result = await _pilotStart.StartPilotAsync(restaurant.Id);
            await _pilotStart.NotifyStartedAsync(restaurant.Id, result);
        }

        private async Task StartPaidSignupPaySessionAsync(
            string email,
            string chosenPlan,
            string chosenCadence,
            Guid pendingSignupId,
            string? customPlanConfigJson
        )
        {
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Email == email
            );
            if (user == null)
            {
                return;
            }

            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(x =>
                x.OwnerUserId == user.Id
            );
            if (restaurant == null)
            {
                return;
            }

            var billing = await _context.BillingAccounts.FirstOrDefaultAsync(
                x => x.RestaurantId == restaurant.Id
            );
            if (billing == null)
            {
                return;
            }

            if (
                string.Equals(
                    chosenPlan,
                    BillingSubscriptionPlans.Custom,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                // A retry does not come from a fresh review. Re-quote the saved config
                // first: an old pack key or a moved price starts no order, and the
                // customer reviews again.
                try
                {
                    await _customPlanCycleBilling.VerifySavedConfigAsync(
                        customPlanConfigJson
                    );
                }
                catch (CustomPlanQuoteException ex)
                {
                    await RecordPendingPaymentAsync(
                        billing,
                        BillingSubscriptionPlans.Custom,
                        chosenCadence,
                        customPlanConfigJson,
                        ex.Code,
                        AdminAuditActions.BillingCustomSignupCheckoutFailed
                    );
                    throw;
                }

                billing.SubscriptionPlan = BillingSubscriptionPlans.Custom;
                billing.CustomPlanConfigJson = customPlanConfigJson;
                billing.BillingCycle = string.Equals(
                    chosenCadence,
                    "annual",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? BillingCycles.Annual
                    : BillingCycles.Monthly;
                await _context.SaveChangesAsync();

                string? checkoutError;
                try
                {
                    var (checkoutUrl, error) =
                        await _customPlanCycleBilling.StartCyclePayAsync(
                            restaurant.Id,
                            BuildSignupCompleteRedirectUrl()
                        );
                    checkoutError = checkoutUrl == null
                        ? error ?? "custom_checkout_missing"
                        : null;
                }
                catch (RevolutMerchantNotReadyException ex)
                {
                    checkoutError = ex.Code;
                }
                catch (InvalidOperationException ex)
                {
                    checkoutError = ex.Message;
                }

                if (checkoutError != null)
                {
                    await RevertUnpaidCustomSignupAsync(
                        billing,
                        chosenCadence,
                        customPlanConfigJson,
                        checkoutError
                    );
                }

                return;
            }

            var locationId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurant.Id)
                .OrderBy(row => row.Id)
                .Select(row => row.Id)
                .FirstOrDefaultAsync();
            if (locationId == 0)
            {
                locationId = restaurant.Id;
            }

            try
            {
                await _firstPaidConversionPaySession.StartAsync(
                    billing,
                    user,
                    restaurant.AccountType,
                    locationId,
                    chosenPlan,
                    chosenCadence,
                    pendingSignupId.ToString("D"),
                    successRedirectUrl: BuildSignupCompleteRedirectUrl()
                );
            }
            catch (RevolutMerchantNotReadyException ex)
            {
                // Stay Free; the owner retries payment from Manage Plan.
                await RecordPendingPaymentAsync(
                    billing,
                    chosenPlan,
                    chosenCadence,
                    customConfigJson: null,
                    ex.Code,
                    AdminAuditActions.BillingPaidSignupCheckoutFailed
                );
            }
            catch (InvalidOperationException ex)
            {
                // Stay Free; the owner retries payment from Manage Plan.
                await RecordPendingPaymentAsync(
                    billing,
                    chosenPlan,
                    chosenCadence,
                    customConfigJson: null,
                    ex.Message,
                    AdminAuditActions.BillingPaidSignupCheckoutFailed
                );
            }
        }

        /// <summary>
        /// Custom checkout did not start at signup. Keep the account on Free and
        /// keep the quote as a pending payment so the owner can retry from Manage Plan.
        /// </summary>
        private async Task RevertUnpaidCustomSignupAsync(
            BillingAccount billing,
            string cadence,
            string? customConfigJson,
            string checkoutError
        )
        {
            billing.SubscriptionPlan = BillingSubscriptionPlans.Free;
            billing.BillingCycle = null;
            billing.CustomPlanConfigJson = null;
            await RecordPendingPaymentAsync(
                billing,
                BillingSubscriptionPlans.Custom,
                cadence,
                customConfigJson,
                checkoutError,
                AdminAuditActions.BillingCustomSignupCheckoutFailed
            );
        }

        /// <summary>
        /// Checkout did not start. The account stays on Free. Save the chosen plan
        /// as a pending payment, log the Revolut code, and audit it for Admin.
        /// </summary>
        private async Task RecordPendingPaymentAsync(
            BillingAccount billing,
            string plan,
            string cadence,
            string? customConfigJson,
            string checkoutError,
            string auditAction
        )
        {
            var code = checkoutError.Length > 64
                ? checkoutError[..64]
                : checkoutError;
            billing.PendingPaymentPlan = plan;
            billing.PendingPaymentCadence = cadence;
            billing.PendingPaymentCustomConfigJson = customConfigJson;
            billing.PendingPaymentFailedAtUtc = DateTime.UtcNow;
            billing.PendingPaymentFailureCode = code;

            _logger.LogWarning(
                "Paid signup checkout did not start. RestaurantId={RestaurantId} Plan={Plan} Code={Code}",
                billing.RestaurantId,
                plan,
                code
            );
            _context.AdminAuditEvents.Add(
                new AdminAuditEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = DateTime.UtcNow,
                    Action = auditAction,
                    ActorIdentity = "system:signup",
                    TargetType = AdminAuditTargetTypes.Restaurant,
                    TargetId = billing.RestaurantId.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    ),
                    RestaurantId = billing.RestaurantId,
                    DetailJson = JsonSerializer.Serialize(
                        new { plan, code }
                    ),
                    Succeeded = false,
                }
            );
            await _context.SaveChangesAsync();
        }

        private string BuildSignupCompleteRedirectUrl()
        {
            // Same base as other Hosted Checkout returns: Revolut rejects
            // localhost / IP hosts, so local dev sets Frontend:RevolutRedirectBaseUrl.
            var baseUrl = RevolutHostedCheckoutRedirectUrls.ResolveRedirectBaseUrl(
                _configuration
            );
            return $"{baseUrl}/signup/complete";
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

        private async Task CreateOperatorAccountAsync(
            ProvisionAccountInput input,
            Action afterUserSaved,
            bool startsPilot
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();
            var provisionedLocations = new List<RestaurantLocation>();
            int? restaurantId = null;
            DateTime? activatedAt = null;
            PilotStartResult? pilotStartResult = null;

            try
            {
                var now = DateTime.UtcNow;
                activatedAt = now;

                var user = new User
                {
                    FullName = input.FullName,
                    Email = input.Email,
                    PasswordHash = input.PasswordHash,
                    PhoneNumber = input.PrimaryPhone ?? string.Empty,
                    Role = "Owner",
                    AccountType = input.AccountType,
                    IsEmailVerified = true,
                    IsApprovedByAdmin = true,
                    IsLocked = false,
                    FailedLoginAttempts = 0,
                    TermsAccepted = input.TermsAccepted,
                    ActivatedAt = now,
                };

                _context.Users.Add(user);
                afterUserSaved();
                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(input.AuthProvider))
                {
                    if (string.IsNullOrWhiteSpace(input.ProviderSubject))
                    {
                        throw new ArgumentException(
                            "Provider subject is required when AuthProvider is set."
                        );
                    }

                    _context.UserExternalLogins.Add(
                        new UserExternalLogin
                        {
                            UserId = user.Id,
                            Provider = input.AuthProvider,
                            ProviderSubject = input.ProviderSubject,
                            CreatedAtUtc = now,
                        }
                    );
                }

                var restaurant = new Restaurant
                {
                    Name = input.GroupName,
                    AccountType = input.AccountType,
                    OwnerUserId = user.Id,
                    BillingContactUserId = user.Id,
                    PrivacyContactUserId = user.Id,
                    SupportContactUserId = user.Id,
                    BusinessCategory = input.BusinessCategory,
                    BusinessLink = input.BusinessLink,
                    PublicPhoneNumber = input.PrimaryPhone,
                    CreatedAt = now,
                    BillingAccount =
                        BillingCreditsService.CreateDefaultBillingAccount(
                            restaurantId: 0,
                            _pricebookCatalog.CurrentPricebookId
                        ),
                };

                _context.Restaurants.Add(restaurant);
                await _context.SaveChangesAsync();

                restaurantId = restaurant.Id;
                user.SelectedRestaurantId = restaurant.Id;

                var scopeError = MembershipLocationScope.Validate(
                    PermissionRoles.Owner,
                    LocationScopeKind.AllLocations,
                    []
                );
                if (scopeError != null)
                {
                    throw new ArgumentException(scopeError);
                }

                _context.RestaurantMemberships.Add(
                    new RestaurantMembership
                    {
                        UserId = user.Id,
                        RestaurantId = restaurant.Id,
                        PermissionRole = PermissionRoles.Owner,
                        LocationScope = LocationScopeKind.AllLocations,
                        NamedLocationIdsJson = "[]",
                        Status = MembershipStatus.Active,
                    }
                );

                foreach (var item in input.Locations)
                {
                    var location = new RestaurantLocation
                    {
                        RestaurantId = restaurant.Id,
                        LocationName = item.LocationName ?? "",
                        Address = item.Address ?? "",
                        City = string.IsNullOrWhiteSpace(item.City)
                            ? null
                            : item.City.Trim(),
                        Postcode = string.IsNullOrWhiteSpace(item.Postcode)
                            ? null
                            : UkPostcode.FormatForDisplay(item.Postcode),
                        LifecycleStatus = LocationLifecycleStatus.Active,
                        LocationPhone = PhoneNumberHelper.NormalizeOptional(
                            item.LocationPhone
                        ),
                        LocalContact = item.LocalContact,
                        CreatedAt = now,
                    };

                    _context.RestaurantLocations.Add(location);
                    provisionedLocations.Add(location);

                    await _qrCodeProvisioning.MintDefaultQrCodesAsync(location);
                }

                await _context.SaveChangesAsync();

                var guestLoop = new GuestLoopSetup
                {
                    RestaurantId = restaurant.Id,
                    SendPhysicalQrMaterials = true,
                    AutoSendReviewRequests = true,
                    CreatedAt = now,
                };

                _context.GuestLoopSetups.Add(guestLoop);

                if (startsPilot)
                {
                    // Joins this transaction. Post-commit side effects run in
                    // NotifyStartedAsync below.
                    pilotStartResult = await _pilotStart.StartPilotAsync(
                        restaurant.Id
                    );
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            if (restaurantId.HasValue && activatedAt.HasValue)
            {
                await _lifecycle.TickAsync(restaurantId.Value, activatedAt.Value);
            }

            // Capture / print-ready starter ensure mints missing starter QR types.
            foreach (var location in provisionedLocations)
            {
                await _printReadyQrMaterialsWork.RequestEnsureAsync(location.Id);
            }

            if (restaurantId.HasValue && pilotStartResult != null)
            {
                await _pilotStart.NotifyStartedAsync(
                    restaurantId.Value,
                    pilotStartResult
                );
            }
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

        private sealed record ProvisionLocationInput(
            string? LocationName,
            string? Address,
            string? City,
            string? Postcode,
            string? LocationPhone,
            string? LocalContact
        );

        private sealed record ProvisionAccountInput(
            string Email,
            string? PasswordHash,
            string FullName,
            string AccountType,
            bool TermsAccepted,
            string GroupName,
            string BusinessCategory,
            string? PrimaryPhone,
            string? BusinessLink,
            IReadOnlyList<ProvisionLocationInput> Locations,
            string? AuthProvider = null,
            string? ProviderSubject = null
        );
    }
}
