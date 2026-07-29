using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IQrCodeProvisioningService
    {
        /// <summary>
        /// Mints and stages (via <c>Add</c>, not yet saved) the five default
        /// Active QR codes for a location — four placement types plus Smart
        /// Guest — each with a freshly minted globally-unique token. Caller
        /// is responsible for calling <c>SaveChangesAsync</c>.
        /// </summary>
        Task<IReadOnlyList<QrCode>> MintDefaultQrCodesAsync(
            RestaurantLocation location
        );
    }
}
