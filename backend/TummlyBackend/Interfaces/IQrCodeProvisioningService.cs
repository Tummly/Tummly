using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IQrCodeProvisioningService
    {
        /// <summary>
        /// Mints and stages (via <c>Add</c>, not yet saved) Active QR codes for
        /// each Published complimentary-starter placement key that the location
        /// does not already have (non-archived). Idempotent — re-Ensure mints
        /// missing types only. Caller is responsible for <c>SaveChangesAsync</c>.
        /// </summary>
        Task<IReadOnlyList<QrCode>> MintDefaultQrCodesAsync(
            RestaurantLocation location
        );
    }
}
