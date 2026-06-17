namespace TummlyBackend.Models
{
    public class OtpVerification
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }

        public string Email { get; set; }

        public string OtpCode { get; set; }

        public bool IsUsed { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}