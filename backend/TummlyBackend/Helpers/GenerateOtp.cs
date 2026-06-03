namespace TummlyBackend.Helpers
{
    public static class GenerateOtp
    {
        public static string CreateOtp()
        {
            Random random = new Random();

            return random
                .Next(100000, 999999)
                .ToString();
        }
    }
}