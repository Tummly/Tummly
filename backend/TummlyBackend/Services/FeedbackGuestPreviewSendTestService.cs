using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackGuestPreviewSendTestService
        : IFeedbackGuestPreviewSendTestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISmartGuestLinkService _smartGuestLink;
        private readonly IConfiguration _configuration;

        public FeedbackGuestPreviewSendTestService(
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

        public async Task<bool?> SendAsync(
            int feedbackId,
            int operatorUserId,
            string? subject,
            string body,
            CancellationToken cancellationToken = default
        )
        {
            var content = FeedbackGuestResponseComposer.ValidateContent(
                FeedbackGuestResponseChannel.Email,
                subject,
                body
            );

            var operatorUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == operatorUserId, cancellationToken);

            if (operatorUser == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (string.IsNullOrWhiteSpace(operatorUser.Email))
            {
                throw new InvalidOperationException(
                    "Operator account email is required for Guest preview send test."
                );
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .Include(f => f.RestaurantLocation!)
                .ThenInclude(l => l.Restaurant)
                .FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);

            if (feedback == null)
            {
                return null;
            }

            var location = feedback.RestaurantLocation
                ?? throw new InvalidOperationException("Feedback location not found.");
            var restaurant = location.Restaurant
                ?? throw new InvalidOperationException("Restaurant not found.");

            var brandTitle = string.IsNullOrWhiteSpace(restaurant.Name)
                ? location.LocationName
                : restaurant.Name.Trim();
            var brandSubtitle =
                string.Equals(
                    brandTitle,
                    location.LocationName,
                    StringComparison.OrdinalIgnoreCase
                )
                    ? null
                    : location.LocationName;

            var giveFeedbackUrl = await ResolveGiveFeedbackUrlAsync(
                location.Id,
                cancellationToken
            );

            await _emailService.SendGuestResponseEmailAsync(
                operatorUser.Email.Trim(),
                content.Subject!,
                brandTitle,
                brandSubtitle,
                location.Address,
                content.Body,
                giveFeedbackUrl
            );

            return true;
        }

        private async Task<string> ResolveGiveFeedbackUrlAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var token = await _smartGuestLink.GetActiveSmartGuestTokenAsync(
                locationId
            );
            if (!string.IsNullOrWhiteSpace(token))
            {
                return _smartGuestLink.BuildGuestUrl(token);
            }

            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/')
                ?? throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured."
                );

            return frontendBaseUrl;
        }
    }
}
