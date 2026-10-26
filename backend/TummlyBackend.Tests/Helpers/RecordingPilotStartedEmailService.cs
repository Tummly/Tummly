namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Counts pilot-started emails. Other email sends are no-ops.
    /// </summary>
    public sealed class RecordingPilotStartedEmailService : EmailServiceStubBase
    {
        public int PilotStartedCalls { get; private set; }

        public override Task SendPilotStartedEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            string pilotEndDateLabel
        )
        {
            PilotStartedCalls++;
            return Task.CompletedTask;
        }
    }
}
