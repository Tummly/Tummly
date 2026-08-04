using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Normalize and validate closed Detected Tag wire keys for operator replace.
    /// </summary>
    public static class DetectedTagSet
    {
        public static bool TryNormalize(
            IReadOnlyList<string>? wireKeys,
            out IReadOnlyList<DetectedTag> tags,
            out string? error
        )
        {
            if (wireKeys is null)
            {
                tags = Array.Empty<DetectedTag>();
                error = "Detected tags are required.";
                return false;
            }

            var parsed = new List<DetectedTag>(wireKeys.Count);
            var seen = new HashSet<DetectedTag>();

            foreach (var key in wireKeys)
            {
                if (!DetectedTagLabels.TryParseKey(key, out var tag))
                {
                    tags = Array.Empty<DetectedTag>();
                    error = $"Unknown detected tag key: {key}.";
                    return false;
                }

                if (!seen.Add(tag))
                {
                    tags = Array.Empty<DetectedTag>();
                    error = $"Duplicate detected tag key: {key}.";
                    return false;
                }

                parsed.Add(tag);
            }

            if (parsed.Contains(DetectedTag.Other) && parsed.Count > 1)
            {
                tags = Array.Empty<DetectedTag>();
                error = "Other cannot be combined with another detected tag.";
                return false;
            }

            parsed.Sort();
            tags = parsed;
            error = null;
            return true;
        }

        public static bool SetsEqual(
            IReadOnlyList<DetectedTag> a,
            IReadOnlyList<DetectedTag> b
        )
            => a.Count == b.Count && a.ToHashSet().SetEquals(b);
    }
}
