using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TummlyBackend.Data
{
    /// <summary>
    /// Design-time factory for <c>dotnet ef</c>. Host DI currently fails
    /// because singleton SignalR publishers consume scoped
    /// <c>IRestaurantPermissionHelper</c> (ticket 14). ADR-0015 CI needs this.
    /// </summary>
    public class ApplicationDbContextFactory
        : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var connection =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Server=127.0.0.1;Database=ci;User Id=ci;Password=ci;TrustServerCertificate=True";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            options.UseSqlServer(connection);
            return new ApplicationDbContext(options.Options);
        }
    }
}
