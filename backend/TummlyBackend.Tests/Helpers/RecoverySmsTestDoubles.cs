using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Provider call that times out. The outcome is unknown: the text may have
    /// been sent.
    /// </summary>
    public sealed class ThrowingRecoveryGuestSmsDelivery : IRecoveryGuestSmsDelivery
    {
        private int _callCount;

        public int CallCount => _callCount;

        public Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            string phoneNumber,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(ref _callCount);
            throw new TimeoutException("The provider did not answer in time.");
        }
    }

    /// <summary>
    /// Live Recovery SMS billing, except that settle is always refused. Reserve and
    /// release go to the real implementation.
    /// </summary>
    public sealed class SettleFailingRecoverySmsBilling : IRecoverySmsBillingReserve
    {
        private readonly IRecoverySmsBillingReserve _inner;

        public SettleFailingRecoverySmsBilling(IRecoverySmsBillingReserve inner)
        {
            _inner = inner;
        }

        public bool IsLive => _inner.IsLive;

        public Task<RecoverySmsBillingReserveResult> ReserveAsync(
            RecoverySmsBillingReserveRequest request,
            CancellationToken cancellationToken = default
        ) => _inner.ReserveAsync(request, cancellationToken);

        public Task<RecoverySmsBillingSettleResult> SettleAsync(
            RecoverySmsBillingSettleRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<RecoverySmsBillingSettleResult>(
            new RecoverySmsBillingSettleResult.Failed
            {
                Message = "Settle refused in test.",
            }
        );

        public Task<RecoverySmsBillingReleaseResult> ReleaseAsync(
            RecoverySmsBillingReleaseRequest request,
            CancellationToken cancellationToken = default
        ) => _inner.ReleaseAsync(request, cancellationToken);
    }

    /// <summary>Fault switches for <see cref="FaultyGuestReplySmsSendStep"/>.</summary>
    public sealed class SmsStepFaults
    {
        /// <summary>Make the idempotency completion throw after the provider accepts.</summary>
        public bool FailComplete { get; set; }

        /// <summary>Make the post-acceptance reply save throw.</summary>
        public bool FailSave { get; set; }
    }

    /// <summary>
    /// The real shared SMS step with faults injected at the two post-acceptance
    /// points. The base class code runs everywhere else.
    /// </summary>
    public sealed class FaultyGuestReplySmsSendStep : GuestReplySmsSendStep
    {
        private readonly SmsStepFaults _faults;

        public FaultyGuestReplySmsSendStep(
            ApplicationDbContext context,
            IRecoverySmsBillingReserve smsBilling,
            IRecoveryGuestSmsDelivery smsDelivery,
            TimeProvider clock,
            ILogger<GuestReplySmsSendStep> logger,
            SmsStepFaults faults
        )
            : base(context, smsBilling, smsDelivery, clock, logger)
        {
            _faults = faults;
        }

        public override Task CompleteAsync(
            GuestReplySmsReservation reservation,
            FeedbackGuestResponse row,
            CancellationToken cancellationToken
        )
        {
            if (_faults.FailComplete)
            {
                throw new InvalidOperationException("Test fault: idempotency completion.");
            }

            return base.CompleteAsync(reservation, row, cancellationToken);
        }

        protected override Task SaveReplyChangesAsync(CancellationToken cancellationToken)
        {
            if (_faults.FailSave)
            {
                throw new InvalidOperationException("Test fault: reply save.");
            }

            return base.SaveReplyChangesAsync(cancellationToken);
        }
    }

    public static class RecoverySmsTestServices
    {
        /// <summary>Registers the faulty step in place of the real one.</summary>
        public static void UseFaultySmsStep(IServiceCollection services, SmsStepFaults faults)
        {
            services.RemoveAll<IGuestReplySmsSendStep>();
            services.AddScoped<IGuestReplySmsSendStep>(sp =>
                new FaultyGuestReplySmsSendStep(
                    sp.GetRequiredService<ApplicationDbContext>(),
                    sp.GetRequiredService<IRecoverySmsBillingReserve>(),
                    sp.GetRequiredService<IRecoveryGuestSmsDelivery>(),
                    sp.GetRequiredService<TimeProvider>(),
                    sp.GetRequiredService<ILogger<GuestReplySmsSendStep>>(),
                    faults
                )
            );
        }
    }
}
