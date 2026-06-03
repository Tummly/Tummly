namespace TummlyBackend.Models
{
    public class PasswordReset
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string ResetToken { get; set; } = string.Empty;

        public bool IsUsed { get; set; } = false;

        public DateTime ExpiryTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /*
         Navigation Property
        */
        public User User { get; set; } = null!;
    }
}