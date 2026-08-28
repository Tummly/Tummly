namespace TummlyBackend.Helpers
{
    /// <summary>
    /// GSM-7 / UCS-2 segment counts for Campaign SMS Reserve (pack v3.0 table).
    /// </summary>
    public static class CampaignSmsSegmentCalculator
    {
        private const int GsmSingleLimit = 160;
        private const int GsmMultipartLimit = 153;
        private const int Ucs2SingleLimit = 70;
        private const int Ucs2MultipartLimit = 67;

        public static int CountSegments(string? messageBody)
        {
            var body = messageBody ?? string.Empty;
            if (body.Length == 0)
            {
                return 1;
            }

            if (IsGsm7(body))
            {
                return body.Length <= GsmSingleLimit
                    ? 1
                    : (int)Math.Ceiling(body.Length / (double)GsmMultipartLimit);
            }

            return body.Length <= Ucs2SingleLimit
                ? 1
                : (int)Math.Ceiling(body.Length / (double)Ucs2MultipartLimit);
        }

        private static bool IsGsm7(string value)
        {
            foreach (var ch in value)
            {
                if (!IsBasicGsm7Character(ch) && !IsGsm7ExtensionCharacter(ch))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsBasicGsm7Character(char ch)
        {
            return ch switch
            {
                >= '\u0020' and <= '\u007E' => true,
                '\n' => true,
                '\r' => true,
                '\f' => true,
                _ => ch switch
                {
                    '£' => true,
                    '€' => true,
                    _ => false,
                },
            };
        }

        private static bool IsGsm7ExtensionCharacter(char ch)
        {
            return ch switch
            {
                '\n' or '\r' or '\f' or '^'
                    or '{' or '}' or '\\' or '[' or '~' or ']' or '|' => true,
                _ => false,
            };
        }
    }
}
