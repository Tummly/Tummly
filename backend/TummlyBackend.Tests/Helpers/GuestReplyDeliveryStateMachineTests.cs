using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class GuestReplyDeliveryStateMachineTests
    {
        private static readonly DateTime At = new(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc);

        private static FeedbackGuestResponse RowIn(GuestReplyDeliveryState state) =>
            new()
            {
                DeliveryState = state,
                DeliveryStateAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            };

        [Fact]
        public void TryAdvance_PendingToAccepted_Moves()
        {
            var row = RowIn(GuestReplyDeliveryState.Pending);

            var moved = GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                GuestReplyDeliveryState.Accepted,
                At
            );

            Assert.True(moved);
            Assert.Equal(GuestReplyDeliveryState.Accepted, row.DeliveryState);
            Assert.Equal(At, row.DeliveryStateAtUtc);
        }

        [Fact]
        public void TryAdvance_AcceptedToDelivered_Moves()
        {
            var row = RowIn(GuestReplyDeliveryState.Accepted);

            var moved = GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                GuestReplyDeliveryState.Delivered,
                At
            );

            Assert.True(moved);
            Assert.Equal(GuestReplyDeliveryState.Delivered, row.DeliveryState);
            Assert.Equal(At, row.DeliveryStateAtUtc);
        }

        [Fact]
        public void TryAdvance_AcceptedAfterDelivered_NoChange()
        {
            var row = RowIn(GuestReplyDeliveryState.Delivered);
            var before = row.DeliveryStateAtUtc;

            var moved = GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                GuestReplyDeliveryState.Accepted,
                At
            );

            Assert.False(moved);
            Assert.Equal(GuestReplyDeliveryState.Delivered, row.DeliveryState);
            Assert.Equal(before, row.DeliveryStateAtUtc);
        }

        [Fact]
        public void TryAdvance_FailedAfterDelivered_NoChange()
        {
            var row = RowIn(GuestReplyDeliveryState.Delivered);
            var before = row.DeliveryStateAtUtc;

            var moved = GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                GuestReplyDeliveryState.Failed,
                At
            );

            Assert.False(moved);
            Assert.Equal(GuestReplyDeliveryState.Delivered, row.DeliveryState);
            Assert.Equal(before, row.DeliveryStateAtUtc);
        }

        [Fact]
        public void TryAdvance_DeliveredAfterFailed_NoChange()
        {
            var row = RowIn(GuestReplyDeliveryState.Failed);
            var before = row.DeliveryStateAtUtc;

            var moved = GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                GuestReplyDeliveryState.Delivered,
                At
            );

            Assert.False(moved);
            Assert.Equal(GuestReplyDeliveryState.Failed, row.DeliveryState);
            Assert.Equal(before, row.DeliveryStateAtUtc);
        }

        [Fact]
        public void TryAdvance_UnavailableToDelivered_Moves()
        {
            var row = RowIn(GuestReplyDeliveryState.Unavailable);

            var moved = GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                GuestReplyDeliveryState.Delivered,
                At
            );

            Assert.True(moved);
            Assert.Equal(GuestReplyDeliveryState.Delivered, row.DeliveryState);
            Assert.Equal(At, row.DeliveryStateAtUtc);
        }

        [Fact]
        public void TryAdvance_UnavailableToAccepted_NoChange()
        {
            var row = RowIn(GuestReplyDeliveryState.Unavailable);
            var before = row.DeliveryStateAtUtc;

            var moved = GuestReplyDeliveryStateMachine.TryAdvance(
                row,
                GuestReplyDeliveryState.Accepted,
                At
            );

            Assert.False(moved);
            Assert.Equal(GuestReplyDeliveryState.Unavailable, row.DeliveryState);
            Assert.Equal(before, row.DeliveryStateAtUtc);
        }

        [Fact]
        public void ToDto_AllValues()
        {
            Assert.Equal("pending", GuestReplyDeliveryStateMachine.ToDto(GuestReplyDeliveryState.Pending));
            Assert.Equal("accepted", GuestReplyDeliveryStateMachine.ToDto(GuestReplyDeliveryState.Accepted));
            Assert.Equal("delivered", GuestReplyDeliveryStateMachine.ToDto(GuestReplyDeliveryState.Delivered));
            Assert.Equal("failed", GuestReplyDeliveryStateMachine.ToDto(GuestReplyDeliveryState.Failed));
            Assert.Equal("unavailable", GuestReplyDeliveryStateMachine.ToDto(GuestReplyDeliveryState.Unavailable));
        }
    }
}
