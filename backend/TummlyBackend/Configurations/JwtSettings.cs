namespace TummlyBackend.Configurations
{
    public class JwtSettings
    {
        /*
         =========================================
         SECRET KEY
         =========================================
        */

        public string Secret { get; set; }

        /*
         =========================================
         ISSUER
         =========================================
        */

        public string Issuer { get; set; }

        /*
         =========================================
         AUDIENCE
         =========================================
        */

        public string Audience { get; set; }

        /*
         =========================================
         EXPIRY IN MINUTES
         =========================================
        */

        public int ExpiryMinutes { get; set; }
    }
}