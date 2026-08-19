using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackGuestPreviewSendTestService
        : IFeedbackGuestPreviewSendTestService
    {
        /// <summary>
        /// Sample redemption code for Guest preview send test — never an issued
        /// Recovery offer code (matches frontend Guest preview placeholder).
        /// </summary>
        public const string SampleRedemptionCode = "PREVIEW-CODE";

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public FeedbackGuestPreviewSendTestService(
            ApplicationDbContext context,
            IEmailService emailService
        )
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<bool?> SendAsync(
            int feedbackId,
            int operatorUserId,
            string? subject,
            string body,
            GuestPreviewTestOfferDto? offer = null,
            string? toEmail = null,
            CancellationToken cancellationToken = default
        )
        {
            var content = FeedbackGuestResponseComposer.ValidateContent(
                FeedbackGuestResponseChannel.Email,
                subject,
                body
            );

            var destination = await ResolveDestinationEmailAsync(
                operatorUserId,
                toEmail,
                cancellationToken
            );

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

            var offerBlock = BuildSampleOfferBlock(offer);

            await _emailService.SendGuestResponseEmailAsync(
                destination,
                content.Subject!,
                brandTitle,
                brandSubtitle,
                location.Address,
                content.Body,
                brandLogoUrl: null,
                offer: offerBlock
            );

            return true;
        }

        private async Task<string> ResolveDestinationEmailAsync(
            int operatorUserId,
            string? toEmail,
            CancellationToken cancellationToken
        )
        {
            var nominated = (toEmail ?? string.Empty).Trim();
            if (nominated.Length > 0)
            {
                return ValidateNominatedEmail(nominated);
            }

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

            return operatorUser.Email.Trim();
        }

        private static string ValidateNominatedEmail(string toEmail)
        {
            if (!IsValidEmail(toEmail))
            {
                throw new ArgumentException("Email address is invalid.");
            }

            return toEmail;
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
            GuestPreviewTestOfferDto? offer
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
