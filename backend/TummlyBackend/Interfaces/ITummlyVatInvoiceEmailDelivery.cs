using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Emails a newly minted Tummly VAT invoice PDF (Billing email / Billing contact).
    /// </summary>
    public interface ITummlyVatInvoiceEmailDelivery
    {
        /// <summary>
        /// Sends when <paramref name="wasNewlyMinted"/> is true. Failures are
        /// logged and do not throw — payment apply must not roll back on mail.
        /// </summary>
        Task DeliverIfNewAsync(
            TummlyVatInvoice invoice,
            bool wasNewlyMinted,
            CancellationToken cancellationToken = default
        );
    }
}
