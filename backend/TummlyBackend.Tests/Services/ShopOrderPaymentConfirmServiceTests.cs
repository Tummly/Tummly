using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class ShopOrderPaymentConfirmServiceTests
    {
        [Fact]
        public async Task Confirm_RevolutCompletedButOrderNotPaid_LogsStableWarning_AndReturnsFalse()
        {
            await using var context = CreateContext();
            var orderId = Guid.NewGuid();
            context.ShopOrders.Add(
                new ShopOrder
                {
                    Id = orderId,
                    OrderNumber = "ORD-1",
                    RestaurantId = 7,
                    LocationId = 1,
                    LocationNameSnapshot = "Main",
                    PlacedByUserId = 1,
                    PlacedByNameSnapshot = "Owner",
                    PaymentStatus = ShopPaymentStatuses.AwaitingPayment,
                    DeliveryMethod = ShopDeliveryMethods.Standard,
                    ShipToContactName = "Ada",
                    ShipToAddressLine1 = "1 High Street",
                    ShipToPostcode = "SE1 1TQ",
                    ShipToCountry = "United Kingdom",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                }
            );
            context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = "ord_rev_1",
                    RestaurantId = 7,
                    ShopOrderId = orderId,
                    Purpose = RevolutOrderIntentPurposes.ShopMaterialsOrder,
                    IsOpen = true,
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
            var merchant = new FakeFirstPaidRevolutMerchantClient();
            merchant.Orders["ord_rev_1"] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: "ord_rev_1",
                State: "completed"
            );
            var applier = new NoOpApplier();
            var logger = new CapturingLogger();
            var service = new ShopOrderPaymentConfirmService(
                context,
                merchant,
                applier,
                NoOpPrintReadyQrMaterialsWork.Instance,
                logger
            );

            var applied = await service.ConfirmAsync(orderId, 7);

            Assert.False(applied);
            Assert.Equal(1, applier.CallCount);
            var entry = Assert.Single(
                logger.Entries,
                row => row.EventName == "completed_not_applied"
            );
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Contains("ord_rev_1", entry.Message);
            Assert.Contains(orderId.ToString(), entry.Message);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed class NoOpApplier : IRevolutOrderCompletedApplier
        {
            public int CallCount { get; private set; }

            public Task ApplyAsync(
                RevolutOrderCompletedApplyRequest request,
                CancellationToken cancellationToken = default
            )
            {
                CallCount++;
                return Task.CompletedTask;
            }
        }

        private sealed class CapturingLogger : ILogger<ShopOrderPaymentConfirmService>
        {
            public List<(LogLevel Level, string Message, string? EventName)> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            ) => Entries.Add((logLevel, formatter(state, exception), eventId.Name));
        }
    }
}
