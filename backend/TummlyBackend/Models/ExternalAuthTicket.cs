using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public static class ExternalAuthTicketPurposes
    {
        public const string OAuthState = "OAuthState";
        public const string SignInExchange = "SignInExchange";
        public const string SignUpExchange = "SignUpExchange";
    }

    public class ExternalAuthTicket
    {
        public Guid Id { get; set; }
        [MaxLength(128)]
        public string Token { get; set; } = ""; // high-entropy; store hashed if preferred — v1 store raw with short TTL
        [MaxLength(32)]
        public string Purpose { get; set; } = "";
        public string PayloadJson { get; set; } = "{}";
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? ConsumedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
