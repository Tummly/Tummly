using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Fail-closed gate before live Revolut Merchant create (customer /
    /// subscription / pay order). Pure Pilot / unpaid paths that never call
    /// Revolut must not use this gate.
    /// </summary>
    public sealed class RevolutMerchantCreateGate : IRevolutMerchantCreateGate
    {
        public const string VatNotReady = "vat_not_ready";

        public const string RevolutNotReady = "revolut_not_ready";

        public const string PlanVariationMissing = "plan_variation_missing";

        private readonly TummlySellerVatSettings _vat;
        private readonly RevolutSettings _revolut;

        public RevolutMerchantCreateGate(
            IOptions<TummlySellerVatSettings> vat,
            IOptions<RevolutSettings> revolut
        )
        {
            _vat = vat.Value;
            _revolut = revolut.Value;
        }

        public string? Evaluate(string? planVariationLookupKey = null)
        {
            if (!_vat.IsComplete)
            {
                return VatNotReady;
            }

            if (!_revolut.HasMerchantApiConfig)
            {
                return RevolutNotReady;
            }

            if (string.IsNullOrWhiteSpace(planVariationLookupKey))
            {
                return null;
            }

            if (
                !_revolut.TryGetPlanVariationId(
                    planVariationLookupKey.Trim(),
                    out _
                )
            )
            {
                return PlanVariationMissing;
            }

            return null;
        }
    }
}
