using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IJwtService
    {
        string GenerateAdminToken(
            Admin admin
        );
        string GenerateToken(
            string userId,
            string email,
            string role
        );

        /// <summary>
        /// Operator JWT with optional extra claims (e.g. product-demo session).
        /// </summary>
        string GenerateToken(
            string userId,
            string email,
            string role,
            IEnumerable<System.Security.Claims.Claim> extraClaims
        );

        /// <summary>
        /// Operator JWT with extra claims and an explicit lifetime (product-demo
        /// sessions get one access token for the whole demo, no refresh).
        /// </summary>
        string GenerateToken(
            string userId,
            string email,
            string role,
            IEnumerable<System.Security.Claims.Claim> extraClaims,
            TimeSpan lifetime
        ) => GenerateToken(userId, email, role, extraClaims);
    }
}