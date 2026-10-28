namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Clock a test moves by hand (<see cref="Advance"/>). The test project has
    /// no <c>Microsoft.Extensions.Time.Testing</c> package.
    /// </summary>
    public sealed class AdjustableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public AdjustableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan by) => _utcNow += by;
    }
}
