using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class TummlyVatInvoiceEmailDelivery : ITummlyVatInvoiceEmailDelivery
    {
        private readonly ApplicationDbContext _context;
        private readonly ITummlyVatInvoiceService _vatInvoices;
        private readonly IEmailService _emailService;
        private readonly ILogger<TummlyVatInvoiceEmailDelivery> _logger;

        public TummlyVatInvoiceEmailDelivery(
            ApplicationDbContext context,
            ITummlyVatInvoiceService vatInvoices,
            IEmailService emailService,
            ILogger<TummlyVatInvoiceEmailDelivery>? logger = null
        )
        {
            _context = context;
            _vatInvoices = vatInvoices;
            _emailService = emailService;
            _logger = logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    TummlyVatInvoiceEmailDelivery
                >.Instance;
        }

        public async Task DeliverIfNewAsync(
            TummlyVatInvoice invoice,
            bool wasNewlyMinted,
            CancellationToken cancellationToken = default
        )
        {
            if (!wasNewlyMinted)
            {
                return;
            }

            try
            {
                await DeliverCoreAsync(invoice, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to email Tummly VAT invoice {DocumentNumber} for restaurant {RestaurantId}",
                    invoice.DocumentNumber,
                    invoice.RestaurantId
                );
            }
        }

        private async Task DeliverCoreAsync(
            TummlyVatInvoice invoice,
            CancellationToken cancellationToken
        )
        {
            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == invoice.RestaurantId,
                    cancellationToken
                );

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == invoice.RestaurantId,
                    cancellationToken
                );
            if (restaurant == null)
            {
                _logger.LogWarning(
                    "Skip invoice email {DocumentNumber}: restaurant {RestaurantId} missing",
                    invoice.DocumentNumber,
                    invoice.RestaurantId
                );
                return;
            }

            var billingContactEmail = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == restaurant.BillingContactUserId)
                .Select(user => user.Email)
                .FirstOrDefaultAsync(cancellationToken);

            var toEmail = TummlyVatInvoiceEmailRecipient.Resolve(
                billingAccount?.BillingEmail,
                billingContactEmail
            );
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning(
                    "Skip invoice email {DocumentNumber}: no Billing email or Billing contact email",
                    invoice.DocumentNumber
                );
                return;
            }

            var pdf = await _vatInvoices.RenderPdfAsync(
                invoice.RestaurantId,
                invoice.DocumentNumber,
                cancellationToken
            );
            if (pdf == null)
            {
                _logger.LogWarning(
                    "Skip invoice email {DocumentNumber}: PDF render returned null",
                    invoice.DocumentNumber
                );
                return;
            }

            await _emailService.SendTummlyVatInvoiceEmailAsync(
                toEmail,
                invoice.DocumentNumber,
                invoice.LineDescription,
                invoice.GrossPence,
                pdf.Value.Content,
                pdf.Value.FileName
            );
        }
    }
}
