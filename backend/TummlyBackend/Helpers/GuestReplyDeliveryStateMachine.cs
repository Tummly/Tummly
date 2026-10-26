using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Forward-only delivery state for guest replies (WS-E R1.3). Rank:
    /// Pending 0 &lt; Accepted, Unavailable 1 &lt; Delivered, Failed 2. A move
    /// applies only when the target rank is higher than the current rank, so
    /// Delivered and Failed stay final and Unavailable never returns to Accepted.
    /// </summary>
    public static class GuestReplyDeliveryStateMachine
    {
        public static bool TryAdvance(
            FeedbackGuestResponse row,
            GuestReplyDeliveryState next,
            DateTime atUtc
        )
        {
            if (Rank(next) <= Rank(row.DeliveryState))
            {
                return false;
            }

            row.DeliveryState = next;
            row.DeliveryStateAtUtc = atUtc;
            return true;
        }

        public static string ToDto(GuestReplyDeliveryState state) =>
            state switch
            {
                GuestReplyDeliveryState.Pending => "pending",
                GuestReplyDeliveryState.Accepted => "accepted",
                GuestReplyDeliveryState.Delivered => "delivered",
                GuestReplyDeliveryState.Failed => "failed",
                GuestReplyDeliveryState.Unavailable => "unavailable",
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
            };

        private static int Rank(GuestReplyDeliveryState state) =>
            state switch
            {
                GuestReplyDeliveryState.Pending => 0,
                GuestReplyDeliveryState.Accepted => 1,
                GuestReplyDeliveryState.Unavailable => 1,
                GuestReplyDeliveryState.Delivered => 2,
                GuestReplyDeliveryState.Failed => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
            };
    }
}
