using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class StubJwtService : IJwtService
    {
        public string GenerateAdminToken(Admin admin) => "admin-token";

        public string GenerateToken(string userId, string email, string role) =>
            $"token:{userId}";

        public string GenerateToken(
            string userId,
            string email,
            string role,
            IEnumerable<System.Security.Claims.Claim> extraClaims
        ) => $"token:{userId}";
    }
}
