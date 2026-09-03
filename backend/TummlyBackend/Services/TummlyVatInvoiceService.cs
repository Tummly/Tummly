using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class TummlyVatInvoiceService : ITummlyVatInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook;
        private readonly TummlySellerVatSettings _sellerVat;

        public TummlyVatInvoiceService(
            ApplicationDbContext context,
            IPricebookCatalog pricebook,
            IOptions<TummlySellerVatSettings> sellerVat
        )
        {
            _context = context;
            _pricebook = pricebook;
            _sellerVat = sellerVat.Value;
        }

        public async Task<TummlyVatInvoice> MintForCompletedOrderAsync(
            TummlyVatInvoiceMintRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var orderId = request.RevolutOrderId.Trim();
            if (string.IsNullOrEmpty(orderId))
            {
                throw new ArgumentException(
                    "Revolut order id is required.",
                    nameof(request)
                );
            }

            var existing = await FindByRevolutOrderIdAsync(
                orderId,
                cancellationToken
            );
            if (existing != null)
            {
                return existing;
            }

            if (!_sellerVat.IsComplete)
            {
                throw new InvalidOperationException(
                    RevolutMerchantCreateGate.VatNotReady
                );
            }

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == request.RestaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                throw new InvalidOperationException("billing_account_missing");
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == request.RestaurantId,
                    cancellationToken
                );
            if (restaurant == null)
            {
                throw new InvalidOperationException("restaurant_missing");
            }

            var business = await _context.RestaurantBusinessDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == request.RestaurantId,
                    cancellationToken
                );

            var netPence = request.NetPenceOverride
                ?? ResolveNetPence(
                    billingAccount.ContractedPricebookId,
                    request.Plan,
                    request.BillingCycle
                );
            var vatRateBps = TummlyVatMath.DefaultVatRateBps;
            var vatPence = TummlyVatMath.VatPenceFromNetPence(netPence, vatRateBps);
            var grossPence = netPence + vatPence;
            var paymentUtc = EnsureUtc(request.PaymentSuccessUtc);
            var year = LondonDateFormat.UkCalendarYear(paymentUtc);
            var documentNumber = await AllocateNextDocumentNumberAsync(
                TummlyDocumentSequence.PrefixTm,
                year,
                cancellationToken
            );

            var lineDescription = string.IsNullOrWhiteSpace(
                request.LineDescriptionOverride
            )
                ? FormatLineDescription(request.Plan, request.BillingCycle)
                : request.LineDescriptionOverride.Trim();

            var lineItems = ResolveLineItems(
                request.LineItems,
                lineDescription,
                quantity: 1,
                netPence,
                vatRateBps
            );

            var invoice = new TummlyVatInvoice
            {
                Id = Guid.NewGuid(),
                DocumentNumber = documentNumber,
                DocumentPrefix = TummlyDocumentSequence.PrefixTm,
                RevolutOrderId = orderId,
                RevolutSubscriptionId = string.IsNullOrWhiteSpace(
                    request.RevolutSubscriptionId
                )
                    ? null
                    : request.RevolutSubscriptionId.Trim(),
                RestaurantId = request.RestaurantId,
                InvoiceDateUtc = paymentUtc,
                TaxPointUtc = paymentUtc,
                LineDescription = lineDescription,
                Quantity = 1,
                NetPence = netPence,
                VatRateBps = vatRateBps,
                VatPence = vatPence,
                GrossPence = grossPence,
                Currency = TummlyVatInvoice.CurrencyGbp,
                PaymentStatus = TummlyVatInvoice.PaymentStatusPaid,
                CustomerBusinessName = ResolveCustomerBusinessName(
                    restaurant,
                    business
                ),
                CustomerAddress = FormatCustomerAddress(business),
                SellerLegalName = _sellerVat.LegalName?.Trim() ?? string.Empty,
                SellerRegisteredAddress =
                    _sellerVat.RegisteredAddress?.Trim() ?? string.Empty,
                SellerVatRegistrationNumber =
                    _sellerVat.RegistrationNumber?.Trim() ?? string.Empty,
                CustomerBillingEmail = ResolveOptionalEmail(
                    request.CustomerBillingEmail,
                    billingAccount.BillingEmail
                ),
                SellerBillingEmail = ResolveOptionalEmail(
                    request.SellerBillingEmail,
                    null
                ),
                DeliverToSnapshot = TrimOrNull(request.DeliverToSnapshot),
                PaymentMethodSummary = TrimOrNull(request.PaymentMethodSummary),
                LineItemsJson = TummlyVatInvoiceLineItems.Serialize(lineItems),
            };

            _context.TummlyVatInvoices.Add(invoice);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return invoice;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                var raced = await FindByRevolutOrderIdAsync(
                    orderId,
                    cancellationToken
                );
                if (raced != null)
                {
                    return raced;
                }

                throw;
            }
        }

        public async Task<TummlyVatInvoice> MintCreditNoteForRefundAsync(
            TummlyVatCreditNoteMintRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var refundOrderId = request.RefundOrderId.Trim();
            if (string.IsNullOrEmpty(refundOrderId))
            {
                throw new ArgumentException(
                    "Refund order id is required.",
                    nameof(request)
                );
            }

            var existing = await FindByRevolutOrderIdAsync(
                refundOrderId,
                cancellationToken
            );
            if (existing != null)
            {
                return existing;
            }

            if (!_sellerVat.IsComplete)
            {
                throw new InvalidOperationException(
                    RevolutMerchantCreateGate.VatNotReady
                );
            }

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == request.RestaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                throw new InvalidOperationException("billing_account_missing");
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == request.RestaurantId,
                    cancellationToken
                );
            if (restaurant == null)
            {
                throw new InvalidOperationException("restaurant_missing");
            }

            var business = await _context.RestaurantBusinessDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == request.RestaurantId,
                    cancellationToken
                );

            var originalInvoice = await FindByRevolutOrderIdAsync(
                request.OriginalPaymentOrderId.Trim(),
                cancellationToken
            );

            var netPence = request.NetPenceOverride
                ?? originalInvoice?.NetPence
                ?? 0;
            var vatRateBps = TummlyVatMath.DefaultVatRateBps;
            var vatPence = TummlyVatMath.VatPenceFromNetPence(netPence, vatRateBps);
            var grossPence = netPence + vatPence;
            var refundUtc = EnsureUtc(request.RefundCompletedUtc);
            var year = LondonDateFormat.UkCalendarYear(refundUtc);
            var documentNumber = await AllocateNextDocumentNumberAsync(
                TummlyDocumentSequence.PrefixTcn,
                year,
                cancellationToken
            );

            var lineDescription = string.IsNullOrWhiteSpace(
                request.LineDescriptionOverride
            )
                ? (
                    originalInvoice != null
                        ? $"Credit note for {originalInvoice.DocumentNumber}"
                        : "Credit note — payment refund"
                )
                : request.LineDescriptionOverride.Trim();

            var lineItems = ResolveLineItems(
                request.LineItems,
                lineDescription,
                quantity: 1,
                netPence,
                vatRateBps
            );

            var creditNote = new TummlyVatInvoice
            {
                Id = Guid.NewGuid(),
                DocumentNumber = documentNumber,
                DocumentPrefix = TummlyDocumentSequence.PrefixTcn,
                RevolutOrderId = refundOrderId,
                RelatedRevolutOrderId = request.OriginalPaymentOrderId.Trim(),
                RevolutSubscriptionId = originalInvoice?.RevolutSubscriptionId,
                RestaurantId = request.RestaurantId,
                InvoiceDateUtc = refundUtc,
                TaxPointUtc = refundUtc,
                LineDescription = lineDescription,
                Quantity = 1,
                NetPence = netPence,
                VatRateBps = vatRateBps,
                VatPence = vatPence,
                GrossPence = grossPence,
                Currency = TummlyVatInvoice.CurrencyGbp,
                PaymentStatus = TummlyVatInvoice.PaymentStatusPaid,
                CustomerBusinessName = ResolveCustomerBusinessName(
                    restaurant,
                    business
                ),
                CustomerAddress = FormatCustomerAddress(business),
                SellerLegalName = _sellerVat.LegalName?.Trim() ?? string.Empty,
                SellerRegisteredAddress =
                    _sellerVat.RegisteredAddress?.Trim() ?? string.Empty,
                SellerVatRegistrationNumber =
                    _sellerVat.RegistrationNumber?.Trim() ?? string.Empty,
                CustomerBillingEmail = ResolveOptionalEmail(
                    request.CustomerBillingEmail,
                    billingAccount.BillingEmail
                        ?? originalInvoice?.CustomerBillingEmail
                ),
                SellerBillingEmail = ResolveOptionalEmail(
                    request.SellerBillingEmail,
                    originalInvoice?.SellerBillingEmail
                ),
                DeliverToSnapshot = TrimOrNull(
                    request.DeliverToSnapshot
                        ?? originalInvoice?.DeliverToSnapshot
                ),
                PaymentMethodSummary = TrimOrNull(
                    request.PaymentMethodSummary
                        ?? originalInvoice?.PaymentMethodSummary
                ),
                LineItemsJson = TummlyVatInvoiceLineItems.Serialize(lineItems),
            };

            _context.TummlyVatInvoices.Add(creditNote);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return creditNote;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                var raced = await FindByRevolutOrderIdAsync(
                    refundOrderId,
                    cancellationToken
                );
                if (raced != null)
                {
                    return raced;
                }

                throw;
            }
        }

        public Task<TummlyVatInvoice?> FindByRevolutOrderIdAsync(
            string revolutOrderId,
            CancellationToken cancellationToken = default
        )
        {
            var orderId = revolutOrderId.Trim();
            return _context.TummlyVatInvoices
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RevolutOrderId == orderId,
                    cancellationToken
                );
        }

        public async Task<IReadOnlyList<InvoiceRowDto>> ListInvoiceRowsForRestaurantAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.TummlyVatInvoices
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.DocumentPrefix == TummlyDocumentSequence.PrefixTm
                )
                .OrderByDescending(row => row.InvoiceDateUtc)
                .ThenByDescending(row => row.DocumentNumber)
                .ToListAsync(cancellationToken);

            return rows.Select(MapInvoiceRow).ToList();
        }

        public async Task<(byte[] Content, string FileName)?> RenderPdfAsync(
            int restaurantId,
            string documentNumber,
            CancellationToken cancellationToken = default
        )
        {
            var trimmed = documentNumber.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            var invoice = await _context.TummlyVatInvoices
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.DocumentNumber == trimmed,
                    cancellationToken
                );
            if (invoice == null)
            {
                return null;
            }

            return (
                TummlyVatInvoicePdfWriter.Render(invoice),
                $"{invoice.DocumentNumber}.pdf"
            );
        }

        public static InvoiceRowDto MapInvoiceRow(TummlyVatInvoice invoice)
        {
            return new InvoiceRowDto
            {
                InvoiceNo = invoice.DocumentNumber,
                InvoiceDateLabel = LondonDateFormat.DMmmYyyy(invoice.InvoiceDateUtc),
                Description = invoice.LineDescription,
                AmountLabel = TummlyVatInvoicePdfWriter.FormatAmountLabel(
                    invoice.GrossPence
                ),
                Status = invoice.PaymentStatus,
                ShowActions = string.Equals(
                    invoice.PaymentStatus,
                    TummlyVatInvoice.PaymentStatusPaid,
                    StringComparison.Ordinal
                ),
            };
        }

        public static string FormatLineDescription(string plan, string billingCycle)
        {
            var cadence = string.Equals(
                billingCycle,
                BillingCycles.Annual,
                StringComparison.OrdinalIgnoreCase
            )
                ? "Annual"
                : "Monthly";
            return $"{plan.Trim()} plan ({cadence})";
        }

        private int ResolveNetPence(
            string contractedPricebookId,
            string plan,
            string billingCycle
        )
        {
            var book = _pricebook.GetRequired(contractedPricebookId);
            var planKey = plan.Trim().ToLowerInvariant();
            if (!book.Plans.TryGetValue(planKey, out var pricebookPlan))
            {
                throw new InvalidOperationException(
                    $"Plan '{plan}' is missing from pricebook '{contractedPricebookId}'."
                );
            }

            return string.Equals(
                billingCycle,
                BillingCycles.Annual,
                StringComparison.OrdinalIgnoreCase
            )
                ? pricebookPlan.AnnualNetPence
                : pricebookPlan.MonthlyNetPence;
        }

        private async Task<string> AllocateNextDocumentNumberAsync(
            string prefix,
            int year,
            CancellationToken cancellationToken
        )
        {
            var sequence = await _context.TummlyDocumentSequences
                .FirstOrDefaultAsync(
                    row => row.DocumentPrefix == prefix && row.Year == year,
                    cancellationToken
                );
            if (sequence == null)
            {
                sequence = new TummlyDocumentSequence
                {
                    DocumentPrefix = prefix,
                    Year = year,
                    NextNumber = 1,
                };
                _context.TummlyDocumentSequences.Add(sequence);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var allocated = sequence.NextNumber;
            sequence.NextNumber = checked(allocated + 1);
            await _context.SaveChangesAsync(cancellationToken);

            return $"{prefix}-{year.ToString(CultureInfo.InvariantCulture)}-{allocated.ToString("D6", CultureInfo.InvariantCulture)}";
        }

        private static string ResolveCustomerBusinessName(
            Restaurant restaurant,
            RestaurantBusinessDetails? business
        )
        {
            if (!string.IsNullOrWhiteSpace(business?.LegalBusinessName))
            {
                return business.LegalBusinessName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(business?.TradingName))
            {
                return business.TradingName.Trim();
            }

            return restaurant.Name?.Trim() ?? string.Empty;
        }

        private static string FormatCustomerAddress(RestaurantBusinessDetails? business)
        {
            if (business == null)
            {
                return string.Empty;
            }

            var parts = new[]
            {
                business.AddressLine1,
                business.AddressLine2,
                business.TownCity,
                business.County,
                business.Postcode,
                business.Country,
            }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim());

            return string.Join(", ", parts);
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }

        private static IReadOnlyList<TummlyVatInvoiceLineItemDto> ResolveLineItems(
            IReadOnlyList<TummlyVatInvoiceLineItemDto>? requested,
            string fallbackTitle,
            int quantity,
            int netPence,
            int vatRateBps
        )
        {
            if (requested is { Count: > 0 })
            {
                return requested;
            }

            return
            [
                new TummlyVatInvoiceLineItemDto(
                    Title: fallbackTitle,
                    Subtitle: null,
                    Quantity: quantity,
                    UnitNetPence: netPence,
                    VatRateBps: vatRateBps,
                    AmountNetPence: netPence
                ),
            ];
        }

        private static string? ResolveOptionalEmail(
            string? preferred,
            string? fallback
        )
        {
            return TrimOrNull(preferred) ?? TrimOrNull(fallback);
        }

        private static string? TrimOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}
