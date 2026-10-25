using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Stable contact key for WS2a suppression hashing:
    /// <c>email:&lt;trim+lower&gt;</c> or <c>phone:&lt;E.164&gt;</c>.
    /// </summary>
    public static class GuestContactKey
    {
        public static bool TryCreate(
            string? contact,
            ContactType type,
            out string key
        )
        {
            key = string.Empty;

            if (type == ContactType.Unknown)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(contact))
            {
                return false;
            }

            if (type == ContactType.Email)
            {
                var normalised = contact.Trim().ToLowerInvariant();
                if (normalised.Length == 0)
                {
                    return false;
                }

                key = "email:" + normalised;
                return true;
            }

            if (type == ContactType.Phone)
            {
                if (
                    !PhoneNumberHelper.TryNormalizeToE164(
                        contact,
                        PhoneNumberHelper.DefaultRegion,
                        out var e164
                    )
                    || string.IsNullOrEmpty(e164)
                )
                {
                    return false;
                }

                key = "phone:" + e164;
                return true;
            }

            return false;
        }

        public static string? FromMasterGuest(MasterGuest master)
        {
            ArgumentNullException.ThrowIfNull(master);

            if (
                TryCreate(
                    master.Email ?? master.NormalizedEmail,
                    ContactType.Email,
                    out var emailKey
                )
            )
            {
                return emailKey;
            }

            if (
                TryCreate(
                    master.Mobile,
                    ContactType.Phone,
                    out var phoneKey
                )
            )
            {
                return phoneKey;
            }

            if (
                TryCreate(
                    master.NormalizedPhone,
                    ContactType.Phone,
                    out phoneKey
                )
            )
            {
                return phoneKey;
            }

            return null;
        }
    }
}
