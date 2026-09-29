using System.Net;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Fail-soft Shop confirmation (Paid) and dispatched (InTransit) emails.
    /// Recipient is the placing user when available, otherwise the restaurant owner.
    /// </summary>
    public sealed class ShopOrderEmailNotifier : IShopOrderEmailNotifier
    {
        private const string TrackingUnavailable = "Available in your order";

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly ILogger<ShopOrderEmailNotifier> _logger;

        public ShopOrderEmailNotifier(
            ApplicationDbContext context,
            IEmailService email,
            ILogger<ShopOrderEmailNotifier>? logger = null
        )
        {
            _context = context;
            _email = email;
            _logger = logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    ShopOrderEmailNotifier
                >.Instance;
        }

        public async Task TryNotifyConfirmedAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        )
        {
            await EmailDispatch.TrySendAsync(
                () => NotifyConfirmedCoreAsync(shopOrderId, cancellationToken),
                _logger,
                "Shop order confirmed email failed for {ShopOrderId}",
                shopOrderId
            );
        }

        public async Task TryNotifyDispatchedAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        )
        {
            await EmailDispatch.TrySendAsync(
                () => NotifyDispatchedCoreAsync(shopOrderId, cancellationToken),
                _logger,
                "Shop order dispatched email failed for {ShopOrderId}",
                shopOrderId
            );
        }

        private async Task NotifyConfirmedCoreAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken
        )
        {
            var snapshot = await LoadSnapshotAsync(shopOrderId, cancellationToken);
            if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.ToEmail))
            {
                return;
            }

            await _email.SendShopOrderConfirmedEmailAsync(
                snapshot.ToEmail,
                snapshot.FirstName,
                snapshot.LocationName,
                snapshot.OrderNumber,
                BuildMaterialsLinesHtml(snapshot.Lines),
                BuildDeliveryAddressHtml(snapshot),
                snapshot.OrderUrl
            );
        }

        private async Task NotifyDispatchedCoreAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken
        )
        {
            var snapshot = await LoadSnapshotAsync(shopOrderId, cancellationToken);
            if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.ToEmail))
            {
                return;
            }

            await _email.SendShopOrderDispatchedEmailAsync(
                snapshot.ToEmail,
                snapshot.FirstName,
                snapshot.LocationName,
                snapshot.OrderNumber,
                DeliveryEstimateLabel(snapshot.DeliveryMethod),
                TrackingLabel(snapshot.TrackingUrl),
                snapshot.OrderUrl
            );
        }

        private async Task<ShopOrderEmailSnapshot?> LoadSnapshotAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken
        )
        {
            var row = await _context.ShopOrders
                .AsNoTracking()
                .Where(order => order.Id == shopOrderId)
                .Select(order => new
                {
                    order.Id,
                    order.OrderNumber,
                    order.LocationId,
                    order.LocationNameSnapshot,
                    order.DeliveryMethod,
                    order.TrackingUrl,
                    order.ShipToContactName,
                    order.ShipToAddressLine1,
                    order.ShipToAddressLine2,
                    order.ShipToPostcode,
                    order.ShipToCountry,
                    order.PlacedByUserId,
                    PlacedByEmail = order.PlacedByUser != null
                        ? order.PlacedByUser.Email
                        : null,
                    PlacedByFullName = order.PlacedByUser != null
                        ? order.PlacedByUser.FullName
                        : null,
                    AccountType = order.Restaurant.AccountType,
                    OwnerEmail = order.Restaurant.OwnerUser != null
                        ? order.Restaurant.OwnerUser.Email
                        : null,
                    OwnerFullName = order.Restaurant.OwnerUser != null
                        ? order.Restaurant.OwnerUser.FullName
                        : null,
                    Lines = order.Lines
                        .OrderBy(line => line.TitleSnapshot)
                        .Select(line => new ShopOrderLineSnapshot(
                            line.TitleSnapshot,
                            line.Quantity
                        ))
                        .ToList(),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (row is null)
            {
                return null;
            }

            var toEmail = !string.IsNullOrWhiteSpace(row.PlacedByEmail)
                ? row.PlacedByEmail.Trim()
                : row.OwnerEmail?.Trim();
            var fullName = !string.IsNullOrWhiteSpace(row.PlacedByFullName)
                ? row.PlacedByFullName
                : row.OwnerFullName;
            var firstName = SignInMetadataResolver.ExtractFirstName(fullName);
            if (string.IsNullOrWhiteSpace(firstName))
            {
                firstName = "there";
            }

            var locationName = string.IsNullOrWhiteSpace(row.LocationNameSnapshot)
                ? "your location"
                : row.LocationNameSnapshot.Trim();

            return new ShopOrderEmailSnapshot(
                ToEmail: toEmail ?? string.Empty,
                FirstName: firstName,
                LocationName: locationName,
                OrderNumber: row.OrderNumber,
                DeliveryMethod: row.DeliveryMethod,
                TrackingUrl: row.TrackingUrl,
                ShipToContactName: row.ShipToContactName,
                ShipToAddressLine1: row.ShipToAddressLine1,
                ShipToAddressLine2: row.ShipToAddressLine2,
                ShipToPostcode: row.ShipToPostcode,
                ShipToCountry: row.ShipToCountry,
                OrderUrl: EmailFrontendUrls.ShopOrder(
                    row.AccountType,
                    row.LocationId,
                    row.Id
                ),
                Lines: row.Lines
            );
        }

        internal static string BuildMaterialsLinesHtml(
            IReadOnlyList<ShopOrderLineSnapshot> lines
        )
        {
            if (lines.Count == 0)
            {
                return WebUtility.HtmlEncode("Materials");
            }

            return string.Join(
                "<br />",
                lines.Select(line =>
                {
                    var title = string.IsNullOrWhiteSpace(line.Title)
                        ? "Material"
                        : line.Title.Trim();
                    return $"{WebUtility.HtmlEncode(title)} × {line.Quantity}";
                })
            );
        }

        internal static string BuildDeliveryAddressHtml(ShopOrderEmailSnapshot snapshot)
        {
            var parts = new List<string>();
            void Add(string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    parts.Add(WebUtility.HtmlEncode(value.Trim()));
                }
            }

            Add(snapshot.ShipToAddressLine1);
            Add(snapshot.ShipToAddressLine2);
            Add(snapshot.ShipToPostcode);
            Add(snapshot.ShipToCountry);

            return parts.Count == 0
                ? WebUtility.HtmlEncode("—")
                : string.Join("<br />", parts);
        }

        internal static string DeliveryEstimateLabel(string? deliveryMethod)
        {
            if (
                string.Equals(
                    deliveryMethod,
                    ShopDeliveryMethods.Express,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "Typically 1–2 working days";
            }

            return "Typically 5–7 working days";
        }

        internal static string TrackingLabel(string? trackingUrl)
        {
            if (string.IsNullOrWhiteSpace(trackingUrl))
            {
                return TrackingUnavailable;
            }

            return trackingUrl.Trim();
        }

        internal sealed record ShopOrderLineSnapshot(string Title, int Quantity);

        internal sealed record ShopOrderEmailSnapshot(
            string ToEmail,
            string FirstName,
            string LocationName,
            string OrderNumber,
            string DeliveryMethod,
            string? TrackingUrl,
            string ShipToContactName,
            string ShipToAddressLine1,
            string? ShipToAddressLine2,
            string ShipToPostcode,
            string ShipToCountry,
            string OrderUrl,
            IReadOnlyList<ShopOrderLineSnapshot> Lines
        );
    }
}
