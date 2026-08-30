namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Shared fail-closed check before live Merchant create calls.
    /// </summary>
    public interface IRevolutMerchantCreateGate
    {
        /// <summary>
        /// Returns <c>vat_not_ready</c>, <c>revolut_not_ready</c>,
        /// <c>revolut_sandbox_required</c>, or <c>plan_variation_missing</c>
        /// when blocked; otherwise null.
        /// Pass a recurring lookup key when the create needs a plan variation;
        /// omit for one-time pay orders / customer create that do not resolve C.
        /// </summary>
        string? Evaluate(string? planVariationLookupKey = null);
    }
}
