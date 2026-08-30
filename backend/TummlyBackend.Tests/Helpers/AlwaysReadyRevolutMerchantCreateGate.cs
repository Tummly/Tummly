using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Test double: Merchant create gate always passes (no VAT / Revolut config).
    /// </summary>
    internal sealed class AlwaysReadyRevolutMerchantCreateGate
        : IRevolutMerchantCreateGate
    {
        public string? Evaluate(string? planVariationLookupKey = null)
        {
            return null;
        }
    }
}
