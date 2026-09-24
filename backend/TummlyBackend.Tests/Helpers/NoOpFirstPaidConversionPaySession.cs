using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class NoOpFirstPaidConversionPaySession
        : IFirstPaidConversionPaySession
    {
        public Task<PlanChangeResultDto> StartAsync(
            BillingAccount billingAccount,
            User owner,
            string restaurantAccountType,
            int locationId,
            string targetPlan,
            string targetCadenceApi,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new PlanChangeResultDto
                {
                    Outcome = "pay",
                    RedirectUrl = "https://checkout.test/noop",
                }
            );

        public Task AbandonOpenSessionsAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }

    public sealed class RecordingFirstPaidConversionPaySession
        : IFirstPaidConversionPaySession
    {
        private readonly ApplicationDbContext _context;

        public RecordingFirstPaidConversionPaySession(
            ApplicationDbContext context
        )
        {
            _context = context;
        }

        public string? LastTargetPlan { get; private set; }

        public string? LastTargetCadence { get; private set; }

        public int AbandonCallCount { get; private set; }

        public const string CheckoutUrl = "https://checkout.test/paid-signup";

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
            LastTargetPlan = targetPlan;
            LastTargetCadence = targetCadenceApi;

            _context.RevolutPendingPaySessions.Add(
                new RevolutPendingPaySession
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = billingAccount.RestaurantId,
                    TargetPlan = targetPlan,
                    TargetCadence = targetCadenceApi,
                    RevolutSubscriptionId = "sub_paid_signup_test",
                    SetupOrderId = "ord_paid_signup_test",
                    CheckoutUrl = CheckoutUrl,
                    IdempotencyKey = idempotencyKey,
                    IsOpen = true,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync(cancellationToken);

            return new PlanChangeResultDto
            {
                Outcome = "pay",
                RedirectUrl = CheckoutUrl,
            };
        }

        public async Task AbandonOpenSessionsAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            AbandonCallCount++;
            var open = await _context.RevolutPendingPaySessions
                .Where(row => row.RestaurantId == restaurantId && row.IsOpen)
                .ToListAsync(cancellationToken);
            foreach (var session in open)
            {
                session.IsOpen = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
