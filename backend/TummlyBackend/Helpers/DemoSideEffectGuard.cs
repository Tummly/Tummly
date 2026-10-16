using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Suppresses real Email/SMS (and related side effects) for the public
    /// product-demo tenant and synthetic seed contacts.
    /// </summary>
    public sealed class DemoSideEffectGuard
    {
        private readonly DemoSessionOptions _options;

        public DemoSideEffectGuard(IOptions<DemoSessionOptions> options)
        {
            _options = options.Value;
        }

        public bool SuppressSideEffectsEnabled =>
            _options.Enabled && _options.SuppressSideEffects;

        /// <summary>
        /// True when outbound must not hit a real provider for this destination.
        /// </summary>
        public bool ShouldSuppressDestination(string? destination)
        {
            if (!SuppressSideEffectsEnabled)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                return false;
            }

            var trimmed = destination.Trim();
            var owner = (_options.OwnerEmail ?? string.Empty).Trim();
            if (
                owner.Length > 0
                && string.Equals(trimmed, owner, StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }

            if (trimmed.Contains('@', StringComparison.Ordinal))
            {
                var lower = trimmed.ToLowerInvariant();
                if (
                    lower.EndsWith(".invalid", StringComparison.Ordinal)
                    || lower.EndsWith(".test", StringComparison.Ordinal)
                    || lower.EndsWith("@example.test", StringComparison.Ordinal)
                    || lower.Contains("@qa-seed.tummly.invalid", StringComparison.Ordinal)
                    || lower.Contains("qa-seed.tummly.invalid", StringComparison.Ordinal)
                )
                {
                    return true;
                }
            }
            else
            {
                // Ofcom UK test numbers used by ProductDemoSeed (owner / staff).
                var digits = new string(trimmed.Where(char.IsDigit).ToArray());
                if (
                    digits is "07700900400" or "07700900401"
                    || digits.EndsWith("7700900400", StringComparison.Ordinal)
                    || digits.EndsWith("7700900401", StringComparison.Ordinal)
                )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// When true, all demo-session outbound should be treated as suppressed
        /// (callers with a demo JWT should pass true).
        /// </summary>
        public bool ShouldSuppressAllDemoOutbound => SuppressSideEffectsEnabled;
    }
}
