namespace TummlyBackend.DTOs.Locations
{
    public abstract record LocationLifecycleWriteResult
    {
        public sealed record Ok(string AccountType) : LocationLifecycleWriteResult;

        public sealed record NotFound : LocationLifecycleWriteResult;

        public sealed record InvalidRequest(string Message)
            : LocationLifecycleWriteResult;

        public sealed record Conflict(string Message)
            : LocationLifecycleWriteResult;
    }
}
