namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Ofcom reserved drama range 07700 900000–900999 (+44 7700 900xxx). It is
    /// for synthetic fixtures, local development and isolated demos only.
    /// Accepted for guest contact and never sent to a provider unless
    /// <c>GuestContact:AllowReservedTestNumbers</c> is true (set once at
    /// startup). Tests use <see cref="OverrideFor"/> so one test cannot leak
    /// its value into another.
    /// </summary>
    public static class ReservedTestNumberPolicy
    {
        public const string ConfigurationKey = "GuestContact:AllowReservedTestNumbers";

        private static volatile bool _startupAllowed;

        private static readonly AsyncLocal<bool?> Override = new();

        public static bool AllowReservedTestNumbers => Override.Value ?? _startupAllowed;

        /// <summary>Sets the process default from configuration at startup.</summary>
        public static void Configure(bool allow) => _startupAllowed = allow;

        /// <summary>
        /// Scoped override for the current async flow. Dispose restores the
        /// previous value.
        /// </summary>
        public static IDisposable OverrideFor(bool allow)
        {
            var previous = Override.Value;
            Override.Value = allow;
            return new Restore(previous);
        }

        private sealed class Restore(bool? previous) : IDisposable
        {
            public void Dispose() => Override.Value = previous;
        }
    }
}
