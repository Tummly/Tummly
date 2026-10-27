using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public class ShopMaterialsOrderPaymentFailureTests
    {
        private static readonly DateTime Now = new(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task ReleaseRequested_ReleasesOpenReservation_StampedWithGivenTime()
        {
            await using var context = CreateContext();
            var (orderId, revolutId) = await SeedAsync(context);

            var changed = await ShopMaterialsOrderPaymentFailure.TryMarkFailedAsync(
                context,
                revolutId,
                CancellationToken.None,
                releaseQrReservations: true,
                nowUtc: Now
            );

            Assert.True(changed);
            var order = await context.ShopOrders.AsNoTracking().SingleAsync(r => r.Id == orderId);
            Assert.Equal(ShopPaymentStatuses.PaymentFailed, order.PaymentStatus);
            Assert.Equal(Now, order.UpdatedAtUtc);
            var reservation = await context.ShopQrSlotReservations
                .AsNoTracking()
                .SingleAsync(r => r.ShopOrderId == orderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Released, reservation.Status);
            Assert.Equal(Now, reservation.UpdatedAtUtc);
        }

        [Fact]
        public async Task ReleaseNotRequested_MarksFailedButKeepsReservationOpen()
        {
            await using var context = CreateContext();
            var (orderId, revolutId) = await SeedAsync(context);

            await ShopMaterialsOrderPaymentFailure.TryMarkFailedAsync(
                context,
                revolutId,
                CancellationToken.None
            );

            var order = await context.ShopOrders.AsNoTracking().SingleAsync(r => r.Id == orderId);
            Assert.Equal(ShopPaymentStatuses.PaymentFailed, order.PaymentStatus);
            Assert.Equal(
                ShopQrSlotReservationStatuses.Open,
                (await context.ShopQrSlotReservations
                    .AsNoTracking()
                    .SingleAsync(r => r.ShopOrderId == orderId)).Status
            );
        }

        private static ApplicationDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static async Task<(Guid OrderId, string RevolutId)> SeedAsync(
            ApplicationDbContext context
        )
        {
            var orderId = Guid.NewGuid();
            var revolutId = $"ord_{Guid.NewGuid():N}";
            var old = Now.AddHours(-1);
            context.ShopOrders.Add(new ShopOrder
            {
                Id = orderId,
                OrderNumber = "PF-1",
                RestaurantId = 1,
                LocationId = 1,
                LocationNameSnapshot = "Site",
                PlacedByNameSnapshot = "Owner",
                PaymentStatus = ShopPaymentStatuses.AwaitingPayment,
                ShipToContactName = "A",
                ShipToAddressLine1 = "1 St",
                ShipToPostcode = "AB1 2CD",
                CreatedAtUtc = old,
                UpdatedAtUtc = old,
            });
            context.RevolutOrderIntents.Add(new RevolutOrderIntent
            {
                Id = Guid.NewGuid(),
                OrderId = revolutId,
                RestaurantId = 1,
                ShopOrderId = orderId,
                Purpose = RevolutOrderIntentPurposes.ShopMaterialsOrder,
                IsOpen = true,
                CreatedAtUtc = old,
            });
            context.ShopQrSlotReservations.Add(new ShopQrSlotReservation
            {
                Id = Guid.NewGuid(),
                ShopOrderId = orderId,
                RestaurantLocationId = 1,
                QrType = QrType.GuestCard,
                Status = ShopQrSlotReservationStatuses.Open,
                CreatedAtUtc = old,
                UpdatedAtUtc = old,
                ReconcileAfterUtc = old.AddMinutes(30),
            });
            await context.SaveChangesAsync();
            return (orderId, revolutId);
        }
    }
}
