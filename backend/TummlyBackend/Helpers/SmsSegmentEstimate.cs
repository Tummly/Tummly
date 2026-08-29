namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Pack GSM-7 / UCS-2 segment estimate (ticket 04 / billing config v3.0).
    /// </summary>
    public static class SmsSegmentEstimate
    {
        public const int Gsm7SingleChars = 160;
        public const int Gsm7ConcatenatedChars = 153;
        public const int Ucs2SingleChars = 70;
        public const int Ucs2ConcatenatedChars = 67;

        public static int EstimateSegments(string messageBody)
        {
            var body = messageBody ?? string.Empty;
            if (body.Length == 0)
            {
                return 1;
            }

            if (IsGsm7(body))
            {
                return body.Length <= Gsm7SingleChars
                    ? 1
                    : (int)Math.Ceiling(body.Length / (double)Gsm7ConcatenatedChars);
            }

            return body.Length <= Ucs2SingleChars
                ? 1
                : (int)Math.Ceiling(body.Length / (double)Ucs2ConcatenatedChars);
        }

        private static bool IsGsm7(string body)
        {
            foreach (var ch in body)
            {
                if (ch is >= '\u0000' and <= '\u007F' or '\u00A0' or '\u00C0' or '\u00C1'
                    or '\u00C2' or '\u00C3' or '\u00C4' or '\u00C5' or '\u00C7' or '\u00C8'
                    or '\u00C9' or '\u00CA' or '\u00CB' or '\u00CC' or '\u00CD' or '\u00CE'
                    or '\u00CF' or '\u00D1' or '\u00D2' or '\u00D3' or '\u00D4' or '\u00D5'
                    or '\u00D6' or '\u00D8' or '\u00D9' or '\u00DA' or '\u00DB' or '\u00DC'
                    or '\u00DF' or '\u00E0' or '\u00E1' or '\u00E2' or '\u00E3' or '\u00E4'
                    or '\u00E5' or '\u00E7' or '\u00E8' or '\u00E9' or '\u00EA' or '\u00EB'
                    or '\u00EC' or '\u00ED' or '\u00EE' or '\u00EF' or '\u00F1' or '\u00F2'
                    or '\u00F3' or '\u00F4' or '\u00F5' or '\u00F6' or '\u00F8' or '\u00F9'
                    or '\u00FA' or '\u00FB' or '\u00FC' or '\u0393' or '\u0394' or '\u0398'
                    or '\u039B' or '\u039E' or '\u03A0' or '\u03A3' or '\u03A6' or '\u03A8'
                    or '\u03A9' or '\u20AC')
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
