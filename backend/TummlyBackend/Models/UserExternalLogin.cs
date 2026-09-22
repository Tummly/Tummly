using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public static class ExternalAuthProviders
    {
        public const string Google = "Google";
        public const string Microsoft = "Microsoft";
    }

    public class UserExternalLogin
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        [MaxLength(32)]
        public string Provider { get; set; } = "";
        [MaxLength(200)]
        public string ProviderSubject { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
