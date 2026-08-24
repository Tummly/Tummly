namespace TummlyBackend.Helpers
{
    public sealed class DuplicateOpenAccountRequestException
        : InvalidOperationException
    {
        public DuplicateOpenAccountRequestException(int existingQueryId)
            : base("An open account request of this kind already exists.")
        {
            ExistingQueryId = existingQueryId;
        }

        public int ExistingQueryId { get; }
    }
}
