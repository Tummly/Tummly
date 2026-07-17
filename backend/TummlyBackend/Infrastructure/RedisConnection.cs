using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace TummlyBackend.Infrastructure
{
    /// <summary>
    /// Shared Redis connection-string helper (ADR-0011).
    /// Presence of ConnectionStrings:Redis opts into the scale-out path;
    /// callers (SignalR backplane today, cache later) reuse this resolve/ping seam.
    /// </summary>
    public static class RedisConnection
    {
        public const string ConnectionStringName = "Redis";

        public static string? TryResolve(IConfiguration configuration)
        {
            var value = configuration
                .GetConnectionString(ConnectionStringName)
                ?.Trim();

            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// Fail fast when Redis is configured but unreachable.
        /// </summary>
        public static void EnsureReachable(string connectionString)
        {
            using var multiplexer = ConnectionMultiplexer.Connect(
                connectionString
            );
            multiplexer.GetDatabase().Ping();
        }
    }
}
