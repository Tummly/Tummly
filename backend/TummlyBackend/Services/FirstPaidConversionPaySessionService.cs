using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Pilot / unpaid Soft lock / Dormant first paid conversion HPP session.
    /// </summary>
    public sealed class FirstPaidConversionPaySessionService
        : IFirstPaidConversionPaySession
    {
        private static readonly HashSet<string> TerminalOrderStates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "completed",
                "cancelled",
                "failed",
                "expired",
            };

        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;
        private readonly IConfiguration _configuration;

        public FirstPaidConversionPaySessionService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            IConfiguration configuration
        )
        {
            _context = context;
            _merchant = merchant;
            _configuration = configuration;
        }

        public async Task<PlanChangeResultDto> StartAsync(
            BillingAccount billingAccount,
            User owner,
            string restaurantAccountType,
            int locationId,
            string targetPlan,
            string targetCadenceApi,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        )
        {
            var lookupKey = RevolutPlanVariationKeys.ForPlanCadence(
                targetPlan,
                targetCadenceApi
            );
            if (lookupKey == null)
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

            _merchant.EnsureReadyForCreate(lookupKey);

            var email = ResolvePayEmail(billingAccount, owner);
            if (email == null)
            {
                throw new InvalidOperationException("billing_email_required");
            }

            var byKey = await _context.RevolutPendingPaySessions
                .Where(row =>
                    row.RestaurantId == billingAccount.RestaurantId
                    && row.IdempotencyKey == idempotencyKey
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (byKey != null)
            {
                if (
                    !SameTarget(
                        byKey.TargetPlan,
                        byKey.TargetCadence,
                        targetPlan,
                        targetCadenceApi
                    )
                )
                {
                    throw new InvalidOperationException(
                        "idempotency_target_mismatch"
                    );
                }

                if (byKey.IsOpen)
                {
                    var reused = await TryReuseCheckoutAsync(
                        byKey,
                        cancellationToken
                    );
                    if (reused != null)
                    {
                        return PayResult(reused);
                    }
                }
            }

            var openPending = await _context.RevolutPendingPaySessions
                .Where(row =>
                    row.RestaurantId == billingAccount.RestaurantId
                    && row.IsOpen
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (openPending != null)
            {
                if (
                    SameTarget(
                        openPending.TargetPlan,
                        openPending.TargetCadence,
                        targetPlan,
                        targetCadenceApi
                    )
                )
                {
                    var reused = await TryReuseCheckoutAsync(
                        openPending,
                        cancellationToken
                    );
                    if (reused != null)
                    {
                        if (
                            !string.Equals(
                                openPending.IdempotencyKey,
                                idempotencyKey,
                                StringComparison.Ordinal
                            )
                        )
                        {
                            openPending.IdempotencyKey = idempotencyKey;
                            await _context.SaveChangesAsync(cancellationToken);
                        }

                        return PayResult(reused);
                    }
                }

                await CancelPendingAsync(openPending, cancellationToken);
            }

            var customerId = await EnsureCustomerAsync(
                billingAccount,
                email,
                owner.FullName,
                cancellationToken
            );

            var redirectUrl = BuildPlanSubscriptionRedirectUrl(
                restaurantAccountType,
                locationId
            );
            var created = await _merchant.CreateSubscriptionAsync(
                new RevolutCreateSubscriptionRequest(
                    customerId,
                    lookupKey,
                    redirectUrl
                ),
                cancellationToken
            );
            if (
                !created.Succeeded
                || string.IsNullOrWhiteSpace(created.Id)
                || string.IsNullOrWhiteSpace(created.SetupOrderId)
            )
            {
                throw new InvalidOperationException(
                    created.ErrorCode ?? "revolut_http_error"
                );
            }

            // Prefer setup_order_checkout_url from create; fall back to order retrieve.
            var checkoutUrl = created.CheckoutUrl;
            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                var order = await _merchant.GetOrderAsync(
                    created.SetupOrderId,
                    cancellationToken
                );
                if (
                    !order.Succeeded
                    || string.IsNullOrWhiteSpace(order.CheckoutUrl)
                )
                {
                    throw new InvalidOperationException(
                        order.ErrorCode ?? "revolut_http_error"
                    );
                }

                checkoutUrl = order.CheckoutUrl;
            }

            var session = new RevolutPendingPaySession
            {
                Id = Guid.NewGuid(),
                RestaurantId = billingAccount.RestaurantId,
                TargetPlan = targetPlan,
                TargetCadence = targetCadenceApi,
                RevolutSubscriptionId = created.Id,
                SetupOrderId = created.SetupOrderId,
                CheckoutUrl = checkoutUrl,
                IdempotencyKey = idempotencyKey,
                IsOpen = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _context.RevolutPendingPaySessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            return PayResult(checkoutUrl);
        }

        private async Task CancelPendingAsync(
            RevolutPendingPaySession pending,
            CancellationToken cancellationToken
        )
        {
            var cancel = await _merchant.CancelSubscriptionAsync(
                pending.RevolutSubscriptionId,
                cancellationToken
            );
            if (!cancel.Succeeded)
            {
                throw new InvalidOperationException(
                    cancel.ErrorCode ?? "revolut_http_error"
                );
            }

            pending.IsOpen = false;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<string?> TryReuseCheckoutAsync(
            RevolutPendingPaySession pending,
            CancellationToken cancellationToken
        )
        {
            var order = await _merchant.GetOrderAsync(
                pending.SetupOrderId,
                cancellationToken
            );
            if (!IsPayableSetupOrder(order))
            {
                pending.IsOpen = false;
                await _context.SaveChangesAsync(cancellationToken);
                return null;
            }

            var url = order.CheckoutUrl!;
            if (
                !string.Equals(
                    pending.CheckoutUrl,
                    url,
                    StringComparison.Ordinal
                )
            )
            {
                pending.CheckoutUrl = url;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return url;
        }

        private async Task<string> EnsureCustomerAsync(
            BillingAccount billingAccount,
            string email,
            string? fullName,
            CancellationToken cancellationToken
        )
        {
            if (!string.IsNullOrWhiteSpace(billingAccount.RevolutCustomerId))
            {
                return billingAccount.RevolutCustomerId;
            }

            var listed = await _merchant.ListCustomersByEmailAsync(
                email,
                cancellationToken
            );
            if (!listed.Succeeded)
            {
                throw new InvalidOperationException(
                    listed.ErrorCode ?? "revolut_http_error"
                );
            }

            if (!string.IsNullOrWhiteSpace(listed.FirstCustomerId))
            {
                billingAccount.RevolutCustomerId = listed.FirstCustomerId;
                await _context.SaveChangesAsync(cancellationToken);
                return listed.FirstCustomerId;
            }

            var created = await _merchant.CreateCustomerAsync(
                new RevolutCreateCustomerRequest(email, fullName),
                cancellationToken
            );
            if (!created.Succeeded || string.IsNullOrWhiteSpace(created.Id))
            {
                throw new InvalidOperationException(
                    created.ErrorCode ?? "revolut_http_error"
                );
            }

            billingAccount.RevolutCustomerId = created.Id;
            await _context.SaveChangesAsync(cancellationToken);
            return created.Id;
        }

        private string BuildPlanSubscriptionRedirectUrl(
            string restaurantAccountType,
            int locationId
        )
        {
            var baseUrl = _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured."
                );
            }

            var root = string.Equals(
                restaurantAccountType,
                "Multi",
                StringComparison.Ordinal
            )
                ? "/multi-dashboard"
                : "/single-dashboard";

            return $"{baseUrl}{root}/settings/billing-credits?location={locationId}&tab=plan-subscription";
        }

        private static string? ResolvePayEmail(
            BillingAccount billingAccount,
            User owner
        )
        {
            if (IsValidEmail(billingAccount.BillingEmail))
            {
                return billingAccount.BillingEmail!.Trim();
            }

            if (IsValidEmail(owner.Email))
            {
                return owner.Email.Trim();
            }

            return null;
        }

        private static bool IsValidEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                var parsed = new MailAddress(value.Trim());
                return parsed.Address.Contains('@');
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool SameTarget(
            string planA,
            string cadenceA,
            string planB,
            string cadenceB
        )
        {
            return string.Equals(planA, planB, StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    cadenceA,
                    cadenceB,
                    StringComparison.OrdinalIgnoreCase
                );
        }

        internal static bool IsPayableSetupOrder(RevolutOrderRetrieveResult order)
        {
            if (
                !order.Succeeded
                || string.IsNullOrWhiteSpace(order.CheckoutUrl)
            )
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(order.State))
            {
                return true;
            }

            return !TerminalOrderStates.Contains(order.State);
        }

        private static PlanChangeResultDto PayResult(string redirectUrl)
        {
            return new PlanChangeResultDto
            {
                Outcome = "pay",
                RedirectUrl = redirectUrl,
            };
        }
    }
}
