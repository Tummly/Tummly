namespace TummlyBackend.Helpers
{
    public static class AddressFormatting
    {
        public static string FormatStreetAndTown(
            string? line1,
            string? line2,
            string? postTown
        )
        {
            var streetParts = new[]
            {
                line1?.Trim(),
                line2?.Trim(),
            }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var street = string.Join(", ", streetParts);
            var town = postTown?.Trim();

            if (string.IsNullOrWhiteSpace(street))
            {
                return town ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(town))
            {
                return street;
            }

            return $"{street}, {town}";
        }

        public static string NormalizeForComparison(string value)
        {
            return new string(
                value
                    .ToLowerInvariant()
                    .Where(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                    .ToArray()
            )
                .Replace("  ", " ", StringComparison.Ordinal)
                .Trim();
        }

        public static bool StreetLinesOverlap(string? left, string? right)
        {
            var normalizedLeft = NormalizeForComparison(left ?? string.Empty);
            var normalizedRight = NormalizeForComparison(right ?? string.Empty);

            if (string.IsNullOrWhiteSpace(normalizedLeft) ||
                string.IsNullOrWhiteSpace(normalizedRight))
            {
                return false;
            }

            return normalizedLeft.Contains(normalizedRight, StringComparison.Ordinal) ||
                   normalizedRight.Contains(normalizedLeft, StringComparison.Ordinal);
        }

        public static string? PickBestMatch(
            IEnumerable<string> candidates,
            string? addressHint
        )
        {
            var list = candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                .ToList();

            if (list.Count == 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(addressHint))
            {
                return list[0];
            }

            var best = list
                .Select(candidate => new
                {
                    Candidate = candidate,
                    Score = ScoreOverlap(candidate, addressHint),
                })
                .OrderByDescending(entry => entry.Score)
                .First();

            return best.Score > 0 ? best.Candidate : list[0];
        }

        private static int ScoreOverlap(string candidate, string addressHint)
        {
            var normalizedCandidate = NormalizeForComparison(candidate);
            var normalizedHint = NormalizeForComparison(addressHint);

            if (normalizedCandidate == normalizedHint)
            {
                return 100;
            }

            if (normalizedCandidate.Contains(normalizedHint, StringComparison.Ordinal) ||
                normalizedHint.Contains(normalizedCandidate, StringComparison.Ordinal))
            {
                return 50;
            }

            var candidateTokens = normalizedCandidate
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.Ordinal);

            var hintTokens = normalizedHint
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return hintTokens.Count(token => candidateTokens.Contains(token));
        }
    }
}
