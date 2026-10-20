using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class FakeFeedbackGuestResponsesService : IFeedbackGuestResponsesService
    {
        public List<(int FeedbackId, string Body)> Sent { get; } = [];

        public HashSet<int> FailFeedbackIds { get; } = [];

        public Task<SendFeedbackGuestResponseResultDto?> SendAsync(
            int feedbackId,
            int authorUserId,
            FeedbackGuestResponseChannel channel,
            FeedbackRecoveryIntent intent,
            string? subject,
            string body,
            string? purpose,
            string? tone,
            string? includeNotes,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default
        )
        {
            if (FailFeedbackIds.Contains(feedbackId))
            {
                throw new InvalidOperationException("send failed");
            }

            Sent.Add((feedbackId, body));
            return Task.FromResult<SendFeedbackGuestResponseResultDto?>(
                new SendFeedbackGuestResponseResultDto
                {
                    WorkflowStatus = "in_progress",
                    NeedsAttention = false,
                    GuestResponse = new FeedbackGuestResponseItemDto
                    {
                        Id = Sent.Count,
                        Channel = channel == FeedbackGuestResponseChannel.Sms
                            ? "sms"
                            : "email",
                        Intent = "respond_to_guest",
                        Body = body,
                        Subject = subject,
                        AuthorDisplayName = "Test",
                        CreatedAt = DateTime.UtcNow,
                    },
                }
            );
        }

        public Task<IReadOnlyList<FeedbackGuestResponseItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
            => Task.FromResult<IReadOnlyList<FeedbackGuestResponseItemDto>>([]);
    }
}
