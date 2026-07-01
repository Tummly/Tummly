using System.Security.Cryptography;
using System.Text;

namespace TummlyBackend.Helpers
{
    public static class ActivationCodeProtectionHelper
    {
        public static string Encrypt(string plainCode, string secretKey)
        {
            var key = DeriveKey(secretKey);
            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            var plainBytes = Encoding.UTF8.GetBytes(plainCode);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(key, tag.Length);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            var payload = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
            Buffer.BlockCopy(
                cipherBytes,
                0,
                payload,
                nonce.Length + tag.Length,
                cipherBytes.Length
            );

            return Convert.ToBase64String(payload);
        }

        public static string? Decrypt(string? ciphertext, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(ciphertext))
            {
                return null;
            }

            try
            {
                var payload = Convert.FromBase64String(ciphertext);
                if (payload.Length < 28)
                {
                    return null;
                }

                var nonce = new byte[12];
                var tag = new byte[16];
                var cipherBytes = new byte[payload.Length - nonce.Length - tag.Length];

                Buffer.BlockCopy(payload, 0, nonce, 0, nonce.Length);
                Buffer.BlockCopy(payload, nonce.Length, tag, 0, tag.Length);
                Buffer.BlockCopy(
                    payload,
                    nonce.Length + tag.Length,
                    cipherBytes,
                    0,
                    cipherBytes.Length
                );

                var plainBytes = new byte[cipherBytes.Length];
                var key = DeriveKey(secretKey);

                using var aes = new AesGcm(key, tag.Length);
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        private static byte[] DeriveKey(string secretKey)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(secretKey));
        }
    }
}
