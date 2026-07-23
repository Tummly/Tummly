using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestIdentityUpdateService : IGuestIdentityUpdateService
    {
        public const int MaxNameLength = 150;
        public const int MaxContactLength = 100;

        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestActivityEmitter _activity;

        public GuestIdentityUpdateService(
            ApplicationDbContext context,
            ILocationGuestActivityEmitter activity
        )
        {
            _context = context;
            _activity = activity;
        }

        public async Task<GuestIdentityUpdateOutcome> UpdateAsync(
            int locationGuestId,
            int locationId,
            PatchGuestIdentityRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var locationGuest = await _context.LocationGuests
                .Include(lg => lg.MasterGuest)
                .FirstOrDefaultAsync(
                    lg =>
                        lg.Id == locationGuestId
                        && lg.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (locationGuest?.MasterGuest == null)
            {
                return GuestIdentityUpdateOutcome.NotFound();
            }

            var joinedName = JoinName(request.FirstName, request.LastName);
            if (joinedName.Length == 0)
            {
                return GuestIdentityUpdateOutcome.ValidationError(
                    "Name is required."
                );
            }

            if (joinedName.Length > MaxNameLength)
            {
                return GuestIdentityUpdateOutcome.ValidationError(
                    $"Name must be at most {MaxNameLength} characters."
                );
            }

            var emailOutcome = NormalizeOptionalEmail(request.Email);
            if (emailOutcome.Error != null)
            {
                return GuestIdentityUpdateOutcome.ValidationError(
                    emailOutcome.Error
                );
            }

            var phoneOutcome = NormalizeOptionalPhone(request.Phone);
            if (phoneOutcome.Error != null)
            {
                return GuestIdentityUpdateOutcome.ValidationError(
                    phoneOutcome.Error
                );
            }

            if (
                emailOutcome.Display == null
                && phoneOutcome.Display == null
            )
            {
                return GuestIdentityUpdateOutcome.ValidationError(
                    "At least one contact channel (email or phone) is required."
                );
            }

            var master = locationGuest.MasterGuest;
            var restaurantId = master.RestaurantId;

            if (emailOutcome.Normalized != null)
            {
                var emailTaken = await _context.MasterGuests
                    .AsNoTracking()
                    .AnyAsync(
                        g =>
                            g.RestaurantId == restaurantId
                            && g.Id != master.Id
                            && g.NormalizedEmail == emailOutcome.Normalized,
                        cancellationToken
                    );

                if (emailTaken)
                {
                    return GuestIdentityUpdateOutcome.IdentityCollision(
                        "Email already belongs to another guest."
                    );
                }
            }

            if (phoneOutcome.Normalized != null)
            {
                var phoneTaken = await _context.MasterGuests
                    .AsNoTracking()
                    .AnyAsync(
                        g =>
                            g.RestaurantId == restaurantId
                            && g.Id != master.Id
                            && g.NormalizedPhone == phoneOutcome.Normalized,
                        cancellationToken
                    );

                if (phoneTaken)
                {
                    return GuestIdentityUpdateOutcome.IdentityCollision(
                        "Phone already belongs to another guest."
                    );
                }
            }

            var changedFields = new List<string>();

            if (!string.Equals(
                    locationGuest.Name,
                    joinedName,
                    StringComparison.Ordinal
                ))
            {
                locationGuest.Name = joinedName;
                changedFields.Add("name");
            }

            var previousEmail = master.Email;
            var previousPhone = master.Mobile;

            if (!string.Equals(
                    previousEmail,
                    emailOutcome.Display,
                    StringComparison.Ordinal
                ))
            {
                master.Email = emailOutcome.Display;
                master.NormalizedEmail = emailOutcome.Normalized;
                changedFields.Add("email");
            }

            if (!string.Equals(
                    previousPhone,
                    phoneOutcome.Display,
                    StringComparison.Ordinal
                ))
            {
                master.Mobile = phoneOutcome.Display;
                master.NormalizedPhone = phoneOutcome.Normalized;
                changedFields.Add("phone");
            }

            if (changedFields.Count > 0)
            {
                _activity.EmitProfileEdited(
                    locationGuest.Id,
                    changedFields,
                    DateTime.UtcNow
                );
            }

            await _context.SaveChangesAsync(cancellationToken);

            return GuestIdentityUpdateOutcome.Updated(
                new PatchGuestIdentityResult
                {
                    Success = true,
                    ChangedFields = changedFields,
                }
            );
        }

        internal static string JoinName(string? firstName, string? lastName)
        {
            var first = (firstName ?? string.Empty).Trim();
            var last = (lastName ?? string.Empty).Trim();

            if (first.Length == 0)
            {
                return last;
            }

            if (last.Length == 0)
            {
                return first;
            }

            return $"{first} {last}";
        }

        private static ContactNormalizeOutcome NormalizeOptionalEmail(
            string? email
        )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ContactNormalizeOutcome.Cleared();
            }

            var trimmed = email.Trim();
            if (trimmed.Length > MaxContactLength)
            {
                return ContactNormalizeOutcome.Failed(
                    $"Email must be at most {MaxContactLength} characters."
                );
            }

            if (!IsValidEmail(trimmed))
            {
                return ContactNormalizeOutcome.Failed(
                    "Please enter a valid email address."
                );
            }

            return ContactNormalizeOutcome.Set(
                trimmed,
                trimmed.ToLowerInvariant()
            );
        }

        private static ContactNormalizeOutcome NormalizeOptionalPhone(
            string? phone
        )
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return ContactNormalizeOutcome.Cleared();
            }

            var trimmed = phone.Trim();
            if (trimmed.Length > MaxContactLength)
            {
                return ContactNormalizeOutcome.Failed(
                    $"Phone must be at most {MaxContactLength} characters."
                );
            }

            if (!PhoneNumberHelper.TryNormalizeToE164(
                    trimmed,
                    PhoneNumberHelper.DefaultRegion,
                    out _
                ))
            {
                return ContactNormalizeOutcome.Failed(
                    "Please enter a valid UK phone number."
                );
            }

            var normalized = new string(
                trimmed.Where(char.IsDigit).ToArray()
            );

            return ContactNormalizeOutcome.Set(trimmed, normalized);
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

        private sealed class ContactNormalizeOutcome
        {
            public string? Display { get; private init; }

            public string? Normalized { get; private init; }

            public string? Error { get; private init; }

            public static ContactNormalizeOutcome Cleared() => new()
            {
                Display = null,
                Normalized = null,
            };

            public static ContactNormalizeOutcome Set(
                string display,
                string normalized
            ) => new()
            {
                Display = display,
                Normalized = normalized,
            };

            public static ContactNormalizeOutcome Failed(string error) => new()
            {
                Error = error,
            };
        }
    }
}
