namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Outcome of <see cref="IPilotStartService.StartPilotAsync"/>.
    /// <c>Started</c> is true only for the call that started the Pilot.
    /// <c>RefusedCode</c> is set when the account cannot start a Pilot.
    /// </summary>
    public sealed record PilotStartResult(
        bool Started,
        string? RefusedCode,
        DateTime? StartedAtUtc,
        DateTime? EndsAtUtc
    );

    /// <summary>
    /// The only writer of the Pilot clock. A restaurant gets one Pilot per
    /// account lifetime (<c>BillingAccount.PilotStartedAtUtc</c>).
    /// </summary>
    public interface IPilotStartService
    {
        /// <summary>
        /// Starts the Pilot for the restaurant. Joins an ambient transaction
        /// when one is open; otherwise opens and commits its own. Does not send
        /// email. Call <see cref="NotifyStartedAsync"/> after the commit.
        /// </summary>
        Task<PilotStartResult> StartPilotAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Post-commit side effects for a Pilot start: queues the complimentary
        /// starter shop orders for every active Location, and sends the
        /// pilot-started email when <c>result.Started</c>. Email errors are
        /// swallowed. Refused results do nothing.
        /// </summary>
        Task NotifyStartedAsync(
            int restaurantId,
            PilotStartResult result,
            CancellationToken cancellationToken = default
        );
    }
}
