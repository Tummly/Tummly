using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    public interface ICampaignSendTestService
    {
        /// <summary>
        /// Sends the current Campaign Email draft as a Guest response email to a
        /// nominated address. Does not burn Email credits, does not use the
        /// Campaign Email adapter, and does not create a Campaign or guest send
        /// fact. When <paramref name="offer"/> is set, includes the offer block
        /// with a sample code only. Failures propagate synchronously (no retry
        /// queue). Returns null when the location is missing.
        /// </summary>
        Task<bool?> SendAsync(
            int locationId,
            string toEmail,
            string? subject,
            string body,
            CampaignSendTestOfferDto? offer = null,
            CancellationToken cancellationToken = default
        );
    }
}
