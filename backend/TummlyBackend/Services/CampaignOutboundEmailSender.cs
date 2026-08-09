using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Production Email outbound for Campaign fire — Accepted when Resend
    /// guest-response send succeeds (Submitted/accepted for settle).
    /// </summary>
    public sealed class CampaignOutboundEmailSender : ICampaignOutboundSender
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly IConfiguration _configuration;

        public CampaignOutboundEmailSender(
            ApplicationDbContext context,
            IEmailService emailService,
            ISmartGuestLinkService smartGuestLink,
            IConfiguration configuration
        )
        {
            _context = context;
            _emailService = emailService;
            _smartGuestLink = smartGuestLink;
            _configuration = configuration;
        }

        public async Task<CampaignOutboundSendResult> SendAsync(
            CampaignOutboundSendRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var channel = (request.Channel ?? string.Empty).Trim().ToLowerInvariant();
            if (channel != "email")
            {
                return new CampaignOutboundSendResult.Rejected
                {
                    Message = "Campaign outbound supports email only in MVP.",
                };
            }

            var campaign = await _context.Campaigns
                .AsNoTracking()
                .Include(c => c.RestaurantLocation!)
                .ThenInclude(l => l.Restaurant)
                .FirstOrDefaultAsync(
                    c => c.Id == request.CampaignId,
                    cancellationToken
                );

            if (campaign?.RestaurantLocation == null)
            {
                return new CampaignOutboundSendResult.Rejected
                {
                    Message = "Campaign location was not found.",
                };
            }

            var location = campaign.RestaurantLocation;
            var restaurant = location.Restaurant;
            var brandTitle =
                restaurant != null && !string.IsNullOrWhiteSpace(restaurant.Name)
                    ? restaurant.Name.Trim()
                    : location.LocationName;
            var brandSubtitle =
                string.Equals(
                    brandTitle,
                    location.LocationName,
                    StringComparison.OrdinalIgnoreCase
                )
                    ? null
                    : location.LocationName;

            try
            {
                var giveFeedbackUrl = await ResolveGiveFeedbackUrlAsync(
                    location.Id
                );
                await _emailService.SendGuestResponseEmailAsync(
                    request.ToAddress,
                    request.Subject ?? string.Empty,
                    brandTitle,
                    brandSubtitle,
                    location.Address,
                    request.Body,
                    giveFeedbackUrl,
                    brandLogoUrl: null,
                    offer: null
                );
                return new CampaignOutboundSendResult.Accepted();
            }
            catch (Exception ex)
            {
                return new CampaignOutboundSendResult.Rejected
                {
                    Message = ex.Message,
                };
            }
        }

        private async Task<string> ResolveGiveFeedbackUrlAsync(int locationId)
        {
            var token = await _smartGuestLink.GetActiveSmartGuestTokenAsync(
                locationId
            );
            if (!string.IsNullOrWhiteSpace(token))
            {
                return _smartGuestLink.BuildGuestUrl(token);
            }

            return _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/')
                ?? "https://tummly.app";
        }
    }
}
