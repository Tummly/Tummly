namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Masks a registrant's email for admin lists. Keeps at most two characters of the
    /// local part and the whole domain, for example <c>ja***@domain.com</c>.
    /// </summary>
    public static class ShopInterestEmailMask
    {
        public const string Placeholder = "***";

        public static string Mask(string? email)
        {
            var trimmed = (email ?? string.Empty).Trim();
            var at = trimmed.IndexOf('@');
            if (at <= 0 || at == trimmed.Length - 1)
            {
                return Placeholder;
            }

            var local = trimmed[..at];
            var domain = trimmed[(at + 1)..];
            var visible = Math.Min(2, local.Length - 1);
            return $"{local[..visible]}{Placeholder}@{domain}";
        }
    }
}
