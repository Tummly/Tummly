namespace TummlyBackend.Services
{
    public sealed class RecoverySmsBillingUnavailableException : Exception
    {
        public RecoverySmsBillingUnavailableException()
            : base("Billing Reserve is not available for Recovery SMS.")
        {
        }
    }

    public sealed class RecoverySmsCreditRefusedException : Exception
    {
        public RecoverySmsCreditRefusedException(
            string code,
            int remaining,
            int requested
        )
            : base(code)
        {
            Code = code;
            Remaining = remaining;
            Requested = requested;
        }

        public string Code { get; }

        public int Remaining { get; }

        public int Requested { get; }
    }
}
