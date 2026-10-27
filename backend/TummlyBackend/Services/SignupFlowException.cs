namespace TummlyBackend.Services
{
    /// <summary>
    /// A signup flow refusal with a stable code and HTTP status. The message is user text.
    /// </summary>
    public sealed class SignupFlowException : Exception
    {
        public string Code { get; }

        public int StatusCode { get; }

        public DateTime? RetryAfterUtc { get; }

        public SignupFlowException(
            string code,
            string message,
            int statusCode = 400,
            DateTime? retryAfterUtc = null
        )
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
            RetryAfterUtc = retryAfterUtc;
        }
    }
}
