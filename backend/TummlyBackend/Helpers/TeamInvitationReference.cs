using System.Security.Cryptography;

namespace TummlyBackend.Helpers
{
    public static class TeamInvitationReference
    {
        public static string Create()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string FirstName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return string.Empty;
            }

            var trimmed = fullName.Trim();
            var space = trimmed.IndexOf(' ');
            return space < 0 ? trimmed : trimmed[..space];
        }

        public static string AcceptUrl(string frontendBaseUrl, string opaqueReference)
        {
            var baseUrl = frontendBaseUrl.Trim().TrimEnd('/');
            return $"{baseUrl}/start?invite={Uri.EscapeDataString(opaqueReference)}";
        }
    }
}
