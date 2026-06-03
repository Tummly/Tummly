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
    }
}