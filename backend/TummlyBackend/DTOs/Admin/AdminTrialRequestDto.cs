namespace TummlyBackend.DTOs.Admin
{
    public class AdminTrialRequestDto
    {
        /*
         =========================================
         REQUEST ID
         =========================================
        */

        public int Id { get; set; }

        /*
         =========================================
         BUSINESS NAME
         =========================================
        */

        public string BusinessName { get; set; }

        /*
         =========================================
         BUSINESS CATEGORY
         =========================================
        */

        public string BusinessCategory { get; set; }

        /*
         =========================================
         LOCATIONS
         =========================================
        */

        public string Locations { get; set; }

        /*
         =========================================
         BUSINESS LINK
         =========================================
        */

        public string? BusinessLink { get; set; }

        /*
         =========================================
         FULL NAME
         =========================================
        */

        public string FullName { get; set; }

        /*
         =========================================
         EMAIL
         =========================================
        */

        public string Email { get; set; }

        /*
         =========================================
         MOBILE
         =========================================
        */

        public string Mobile { get; set; }

        /*
         =========================================
         ROLE
         =========================================
        */

        public string Role { get; set; }

        /*
         =========================================
         BUSINESS GOAL
         =========================================
        */

        public string Goal { get; set; }

        /*
         =========================================
         EMAIL VERIFIED
         =========================================
        */

        public bool IsEmailVerified { get; set; }

        /*
         =========================================
         APPROVED
         =========================================
        */

        public bool IsApproved { get; set; }

        /*
         =========================================
         ACCOUNT TYPE
         =========================================
        */

        public string AccountType { get; set; }

        /*
         =========================================
         ADMIN STATUS
         =========================================
        */

        public string Status { get; set; }

        /*
         =========================================
         CREATED DATE
         =========================================
        */

        public DateTime CreatedAt { get; set; }
    }
}