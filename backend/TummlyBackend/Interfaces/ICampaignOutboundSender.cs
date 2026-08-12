using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Provider send for one Campaign recipient (Email first). Tests inject fakes.
    /// </summary>
    public interface ICampaignOutboundSender
    {
        Task<CampaignOutboundSendResult> SendAsync(
            CampaignOutboundSendRequest request,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class CampaignOutboundSendRequest
    {
        public required int CampaignId { get; init; }

        public required int LocationGuestId { get; init; }

        public required string Channel { get; init; }

        public required string ToAddress { get; init; }

        public required string? Subject { get; init; }

        public required string Body { get; init; }

        /// <summary>
        /// When set, guest email includes Offer claim QR + code and omits Give Feedback.
        /// </summary>
        public GuestResponseEmailOfferBlock? Offer { get; init; }
    }

    public abstract class CampaignOutboundSendResult
    {
        private CampaignOutboundSendResult()
        {
        }

        /// <summary>Provider accepted the unit - settle one credit.</summary>
        public sealed class Accepted : CampaignOutboundSendResult
        {
        }

        /// <summary>Provider rejected; do not settle.</summary>
        public sealed class Rejected : CampaignOutboundSendResult
        {
            public required string Message { get; init; }
        }
    }
}
