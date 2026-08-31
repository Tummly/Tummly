namespace TummlyBackend.DTOs.Locations
{
    public sealed class SetLocationManagerRequest
    {
        public int? ManagerUserId { get; set; }
    }

    public abstract record LocationLifecycleWriteResult
    {
        public sealed record Ok : LocationLifecycleWriteResult;

        public sealed record NotFound : LocationLifecycleWriteResult;

        public sealed record InvalidRequest(string Message)
            : LocationLifecycleWriteResult;

        public sealed record Conflict(string Message)
            : LocationLifecycleWriteResult;
    }
}
