using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface ICustomPlanQuoteService
    {
        /// <summary>
        /// Quote a v2 custom plan from the published commercial catalog.
        /// Throws <see cref="CustomPlanQuoteException"/> on validation errors.
        /// </summary>
        Task<CustomPlanQuoteResult> QuoteAsync(
            CustomPlanQuoteRequest request,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class CustomPlanQuoteException : Exception
    {
        public string Code { get; }

        public CustomPlanQuoteException(string code, string message)
            : base(message)
        {
            Code = code;
        }
    }
}
