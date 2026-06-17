namespace TummlyBackend.Models
{
    public class OtpVerification
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }

        // '?' lagane se ye ab nullable ho gaya hai, warning khatam ho jayegi
        public string? Email { get; set; }

        public string? OtpCode { get; set; }

        public bool IsUsed { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        // Agar aapko 'ChannelEmail' ki zaroorat hai (jo error de raha tha), 
        // toh use yahan aise add karein:
        public string? ChannelEmail { get; set; }
    }
}