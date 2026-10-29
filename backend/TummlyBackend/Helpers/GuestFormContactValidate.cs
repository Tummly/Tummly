using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Guest Form submit contact: valid Email or UK mobile only.
    /// Does not rewrite the contact string.
    /// </summary>
    public static class GuestFormContactValidate
    {
        /// <summary>
        /// True when contact is a valid Email or UK mobile (landlines rejected).
        /// Sets contactType to Email or Phone. Does not rewrite the contact string.
        /// </summary>
        public static bool TryResolve(
            string? contact,
            out ContactType contactType
        )
        {
            contactType = ContactType.Unknown;

            if (string.IsNullOrWhiteSpace(contact))
            {
                return false;
            }

            var trimmed = contact.Trim();

            if (trimmed.Contains('@'))
            {
                if (!IsSingleValidEmail(trimmed))
                {
                    return false;
                }

                contactType = ContactType.Email;
                return true;
            }

            if (
                PhoneNumberHelper.TryNormalizeUkMobileToE164(
                    trimmed,
                    PhoneNumberHelper.DefaultRegion,
                    out _
                )
            )
            {
                contactType = ContactType.Phone;
                return true;
            }

            return false;
        }

        private static bool IsSingleValidEmail(string trimmed)
        {
            if (
                trimmed.Any(char.IsWhiteSpace)
                || trimmed.Contains(',')
                || trimmed.Contains(';')
            )
            {
                return false;
            }

            var atCount = 0;
            foreach (var ch in trimmed)
            {
                if (ch == '@')
                {
                    atCount++;
                }
            }

            if (atCount != 1)
            {
                return false;
            }

            if (!new EmailAddressAttribute().IsValid(trimmed))
            {
                return false;
            }

            return MailAddress.TryCreate(trimmed, out var mail)
                && mail.Address == trimmed;
        }
    }
}
