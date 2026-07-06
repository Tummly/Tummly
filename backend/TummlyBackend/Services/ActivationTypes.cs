namespace TummlyBackend.Services
{
    public enum ActivationIntent
    {
        SignIn,
        ApiAccess,
    }

    public enum ActivationOutcome
    {
        Allow,
        Block,
    }

    public enum ActivationReason
    {
        None,
        Pending,
        Expired,
    }

    public sealed record ActivationDecision(
        ActivationOutcome Outcome,
        ActivationReason Reason,
        string Message
    );

    public class ActivationExpiredException : Exception
    {
        public ActivationExpiredException(string message)
            : base(message)
        {
        }
    }
}
