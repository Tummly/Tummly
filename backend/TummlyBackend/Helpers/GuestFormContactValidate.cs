using System.ComponentModel.DataAnnotations;
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
        /// True when contact is a valid Email or UK mobile.
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

            if (
                trimmed.Contains('@')
                && new EmailAddressAttribute().IsValid(trimmed)
            )
            {
                contactType = ContactType.Email;
                return true;
            }

            if (
                PhoneNumberHelper.TryNormalizeToE164(
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
    }
}
