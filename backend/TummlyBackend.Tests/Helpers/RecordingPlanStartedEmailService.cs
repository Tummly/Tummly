namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Records plan-started emails. Other email sends are no-ops.
    /// </summary>
    public sealed class RecordingPlanStartedEmailService : EmailServiceStubBase
    {
        public List<(string ToEmail, string PlanName, string RestaurantName)> PlanStartedCalls { get; } = [];

        public override Task SendPlanStartedEmailAsync(
            string toEmail,
            string firstName,
            string planName,
            string restaurantName
        )
        {
            PlanStartedCalls.Add((toEmail, planName, restaurantName));
            return Task.CompletedTask;
        }
    }
}
