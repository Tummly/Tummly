namespace TummlyBackend.DTOs.Signup
{
    public sealed class SignupSessionLoginDto
    {
        public Guid SessionToken { get; set; }

        /// <summary>
        /// Paid checkout failed: sign in on the Free plan instead of waiting
        /// for payment. Ignored unless a payment failure is recorded.
        /// </summary>
        public bool ContinueOnFree { get; set; }
    }
}
