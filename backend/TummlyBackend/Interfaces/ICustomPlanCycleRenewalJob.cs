namespace TummlyBackend.Interfaces
{
    public interface ICustomPlanCycleRenewalJob
    {
        Task<CustomPlanCycleRenewalJobResult> ProcessAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class CustomPlanCycleRenewalJobResult
    {
        public int Processed { get; init; }

        public int OrdersOpened { get; init; }

        public int Failed { get; init; }
    }
}
