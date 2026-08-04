using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class DetectedTagSetTests
    {
        [Fact]
        public void TryNormalize_AcceptsEmptySet()
        {
            var ok = DetectedTagSet.TryNormalize(
                Array.Empty<string>(),
                out var tags,
                out var error
            );

            Assert.True(ok);
            Assert.Null(error);
            Assert.Empty(tags);
        }

        [Fact]
        public void TryNormalize_AcceptsOtherAlone()
        {
            var ok = DetectedTagSet.TryNormalize(
                new[] { "Other" },
                out var tags,
                out var error
            );

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(new[] { DetectedTag.Other }, tags);
        }

        [Fact]
        public void TryNormalize_RejectsDuplicates()
        {
            var ok = DetectedTagSet.TryNormalize(
                new[] { "Service", "FoodQuality", "Service" },
                out var tags,
                out var error
            );

            Assert.False(ok);
            Assert.Empty(tags);
            Assert.Contains("Duplicate", error, StringComparison.Ordinal);
        }

        [Fact]
        public void TryNormalize_AcceptsMultipleNonOtherTags_Sorted()
        {
            var ok = DetectedTagSet.TryNormalize(
                new[] { "Service", "FoodQuality" },
                out var tags,
                out var error
            );

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(
                new[] { DetectedTag.FoodQuality, DetectedTag.Service },
                tags
            );
        }

        [Fact]
        public void TryNormalize_RejectsUnknownKey()
        {
            var ok = DetectedTagSet.TryNormalize(
                new[] { "FoodQuality", "NotATag" },
                out var tags,
                out var error
            );

            Assert.False(ok);
            Assert.Empty(tags);
            Assert.Contains("unknown", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TryNormalize_RejectsOtherCombinedWithAnotherTag()
        {
            var ok = DetectedTagSet.TryNormalize(
                new[] { "Other", "Service" },
                out var tags,
                out var error
            );

            Assert.False(ok);
            Assert.Empty(tags);
            Assert.Contains("Other", error, StringComparison.Ordinal);
        }

        [Fact]
        public void TryNormalize_RejectsNullWireList()
        {
            var ok = DetectedTagSet.TryNormalize(
                null,
                out var tags,
                out var error
            );

            Assert.False(ok);
            Assert.Empty(tags);
            Assert.NotNull(error);
        }

        [Fact]
        public void SetsEqual_IgnoresOrder()
        {
            Assert.True(
                DetectedTagSet.SetsEqual(
                    new[] { DetectedTag.Service, DetectedTag.FoodQuality },
                    new[] { DetectedTag.FoodQuality, DetectedTag.Service }
                )
            );
            Assert.False(
                DetectedTagSet.SetsEqual(
                    new[] { DetectedTag.Service },
                    new[] { DetectedTag.FoodQuality }
                )
            );
        }
    }
}
