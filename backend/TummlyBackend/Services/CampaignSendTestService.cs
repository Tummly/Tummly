using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class CampaignSendTestService : ICampaignSendTestService
    {
        /// <summary>
        /// Sample redemption code for Campaign send test — never an issued offer
        /// code (matches Guest preview placeholder).
        /// </summary>
        public const string SampleRedemptionCode = "PREVIEW-CODE";

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ICampaignProductAnalytics _analytics;

        public CampaignSendTestService(
            ApplicationDbContext context,
            IEmailService emailService,
            ICampaignProductAnalytics? analytics = null
        )
        {
            _context = context;
            _emailService = emailService;
            _analytics = analytics ?? NoOpCampaignProductAnalytics.Instance;
        }

        public async Task<bool?> SendAsync(
            int locationId,
            string toEmail,
            string? subject,
            string body,
            CampaignSendTestOfferDto? offer = null,
            CancellationToken cancellationToken = default
        )
        {
            var nominatedEmail = ValidateNominatedEmail(toEmail);

            var content = FeedbackGuestResponseComposer.ValidateContent(
                FeedbackGuestResponseChannel.Email,
                subject,
                body
            );

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .Include(l => l.Restaurant)
                .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

            if (location == null)
            {
                return null;
            }

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

            var offerBlock = BuildSampleOfferBlock(offer);

            await _emailService.SendGuestResponseEmailAsync(
                nominatedEmail,
                content.Subject!,
                brandTitle,
                brandSubtitle,
                location.Address,
                content.Body,
                brandLogoUrl: null,
                offer: offerBlock
            );

            _analytics.TrackSendTest(locationId);

            return true;
        }

        private static string ValidateNominatedEmail(string? toEmail)
        {
            var trimmed = (toEmail ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Email address is required.");
            }

            if (!IsValidEmail(trimmed))
            {
                throw new ArgumentException("Email address is invalid.");
            }

            return trimmed;
        }

        private static bool IsValidEmail(string email)
        {
            if (new EmailAddressAttribute().IsValid(email) != true)
            {
                return false;
            }

            try
            {
                var parsed = new MailAddress(email);
                return string.Equals(
                    parsed.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase
                );
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static GuestResponseEmailOfferBlock? BuildSampleOfferBlock(
            CampaignSendTestOfferDto? offer
        )
        {
            if (offer is null)
            {
                return null;
            }

            var title = (offer.Title ?? string.Empty).Trim();
            if (title.Length == 0)
            {
                return null;
            }

            return new GuestResponseEmailOfferBlock(
                Title: title,
                Description: (offer.Description ?? string.Empty).Trim(),
                RedemptionCode: SampleRedemptionCode,
                ExpiryLabel: string.IsNullOrWhiteSpace(offer.ExpiryLabel)
                    ? "Expires: —"
                    : offer.ExpiryLabel.Trim()
            );
        }
    }
}
