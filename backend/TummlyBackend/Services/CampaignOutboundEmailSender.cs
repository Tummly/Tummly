using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
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
        private readonly IConfiguration _configuration;

        public CampaignOutboundEmailSender(
            ApplicationDbContext context,
            IEmailService emailService,
            IConfiguration configuration
        )
        {
            _context = context;
            _emailService = emailService;
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
                    Message = "CampaignOutboundEmailSender accepts email only.",
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
            var brandTitle = CampaignSenderDisplayName.Resolve(
                restaurant,
                location.LocationName
            );
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
                var brandLogoUrl = BrandLogoRules.BuildAbsolutePublicUrl(
                    restaurant?.BrandLogoObjectKey,
                    _configuration
                );

                var frontendBaseUrl =
                    _configuration["Frontend:BaseUrl"] ?? string.Empty;
                var unsubscribeHref = UnsubscribeLink.PreferSignedOrRestaurant(
                    frontendBaseUrl,
                    restaurant?.Id ?? 0,
                    request.LocationGuestId,
                    UnsubscribeLink.ResolveSigningSecret(_configuration)
                );

                var subject =
                    request.Offer is not null
                        ? CampaignEmailSubject.Format(
                            request.Offer.Title,
                            brandTitle
                        )
                        : (request.Subject ?? string.Empty);

                var tags = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["campaign_id"] = request.CampaignId.ToString(),
                    ["location_guest_id"] = request.LocationGuestId.ToString(),
                };
                var providerMessageId =
                    await _emailService.SendCampaignGuestEmailAsync(
                        request.ToAddress,
                        subject,
                        brandTitle,
                        brandSubtitle,
                        location.Address,
                        request.Body,
                        brandLogoUrl: brandLogoUrl,
                        offer: request.Offer,
                        unsubscribeHref: unsubscribeHref,
                        // Offer unlocked Figma: message only in the ticket (no subject line).
                        ticketSubject: request.Offer is not null
                            ? string.Empty
                            : null,
                        tags: tags
                    );
                return new CampaignOutboundSendResult.Accepted
                {
                    AcceptedUnits = 1,
                    ProviderMessageId = providerMessageId,
                };
            }
            catch (Exception ex)
            {
                return new CampaignOutboundSendResult.Rejected
                {
                    Message = ex.Message,
                };
            }
        }
    }
}
