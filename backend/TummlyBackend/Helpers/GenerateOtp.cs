using System.Security.Cryptography;

namespace TummlyBackend.Helpers
{
    public static class GenerateOtp
    {
        /// <summary>Six-digit code from a CSPRNG, zero-padded (e.g. "012345").</summary>
        public static string CreateOtp()
        {
            return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }
    }
}
