using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class BulkRecoveryBatchMapping
    {
        public static string ToWireStatus(BulkRecoveryBatchStatus status)
            => status switch
            {
                BulkRecoveryBatchStatus.Draft => "draft",
                BulkRecoveryBatchStatus.Completed => "completed",
                BulkRecoveryBatchStatus.Partial => "partial",
                BulkRecoveryBatchStatus.Failed => "failed",
                _ => "draft",
            };

        public static string ToWireMemberStatus(BulkRecoveryBatchMemberStatus status)
            => status switch
            {
                BulkRecoveryBatchMemberStatus.Pending => "pending",
                BulkRecoveryBatchMemberStatus.Sent => "sent",
                BulkRecoveryBatchMemberStatus.Skipped => "skipped",
                BulkRecoveryBatchMemberStatus.Failed => "failed",
                _ => "pending",
            };

        public static BulkRecoveryBatchDto ToDto(
            BulkRecoveryBatch batch,
            IReadOnlyList<BulkRecoveryBatchMemberDto> members
        )
            => new()
            {
                Id = batch.Id,
                RestaurantId = batch.RestaurantId,
                LocationId = batch.RestaurantLocationId,
                Channel = FeedbackGuestResponseMapping.ToWireChannel(batch.Channel),
                Intent = FeedbackGuestResponseMapping.ToWireIntent(batch.Intent),
                Subject = batch.Subject,
                BodyTemplate = batch.BodyTemplate,
                Purpose = batch.Purpose,
                Tone = batch.Tone,
                IncludeNotes = batch.IncludeNotes,
                Status = ToWireStatus(batch.Status),
                MemberCount = members.Count,
                CreatedAt = batch.CreatedAt,
                CompletedAt = batch.CompletedAt,
                Members = members,
            };

        public static BulkRecoveryBatchMemberDto ToMemberDto(
            BulkRecoveryBatchMember member,
            Feedback feedback
        )
        {
            var masked = string.IsNullOrWhiteSpace(feedback.GuestContact)
                ? "••••"
                : FeedbackGuestResponseMapping.MaskDestination(
                    feedback.ContactType,
                    feedback.GuestContact
                );
            return new BulkRecoveryBatchMemberDto
            {
                Id = member.Id,
                FeedbackId = member.FeedbackId,
                GuestName = feedback.GuestName ?? string.Empty,
                MaskedDestination = masked,
                FeedbackComment = TruncateComment(feedback.Comment),
                Status = ToWireMemberStatus(member.Status),
                FeedbackGuestResponseId = member.FeedbackGuestResponseId,
                FailureReason = member.FailureReason,
            };
        }

        private static string? TruncateComment(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                return null;
            }

            var trimmed = comment.Trim();
            return trimmed.Length <= 240 ? trimmed : trimmed[..240] + "…";
        }
    }
}
