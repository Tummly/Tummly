namespace TummlyBackend.Models
{
    public class AccountSetupInvite
    {
        public int Id { get; set; }

        /*
         =========================================
         EMAIL
         =========================================
        */

        public string Email { get; set; }

        /*
         =========================================
         INVITE TOKEN
         =========================================
        */

        public string InviteToken { get; set; }

        /*
         =========================================
         ACCOUNT TYPE
         =========================================
        */

        public string AccountType { get; set; }

        /*
         =========================================
         IS USED
         =========================================
        */

        public bool IsUsed { get; set; } = false;

        /*
         =========================================
         EXPIRY DATE
         =========================================
        */

        public DateTime ExpiresAt { get; set; }

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}