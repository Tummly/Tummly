using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class GuestScopedListValidationTests
    {
        [Theory]
        [InlineData(0, 25, "recent-activity", "page must be >= 1.")]
        [InlineData(1, 20, "recent-activity", "pageSize must be 25.")]
        [InlineData(1, 25, "newest", "sort must be recent-activity or oldest-first.")]
        public void ValidatePagingAndSort_RejectsInvalidValues(
            int page,
            int pageSize,
            string sort,
            string message
        )
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                GuestScopedListValidation.ValidatePagingAndSort(
                    page,
                    pageSize,
                    sort,
                    25
                )
            );

            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void ValidatePagingAndSort_NormalizesValidSort()
        {
            var result = GuestScopedListValidation.ValidatePagingAndSort(
                1,
                25,
                " Oldest-First ",
                25
            );

            Assert.Equal("oldest-first", result);
        }

        [Fact]
        public void ResolveOptionalDateWindow_RejectsMixedPresetAndCustom()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                GuestScopedListValidation.ResolveOptionalDateWindow(
                    "last-7",
                    DateTime.UtcNow.AddDays(-1),
                    null,
                    0
                )
            );

            Assert.Equal(
                "datePreset and dateFrom/dateTo are mutually exclusive.",
                exception.Message
            );
        }

        [Fact]
        public void ResolveOptionalDateWindow_UsesProvidedClockForPreset()
        {
            var (from, to) =
                GuestScopedListValidation.ResolveOptionalDateWindow(
                    "today",
                    null,
                    null,
                    60,
                    new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc)
                );

            Assert.Equal(
                new DateTime(2026, 7, 22, 23, 0, 0, DateTimeKind.Utc),
                from
            );
            Assert.Equal(
                new DateTime(2026, 7, 23, 23, 0, 0, DateTimeKind.Utc),
                to
            );
        }
    }
}
