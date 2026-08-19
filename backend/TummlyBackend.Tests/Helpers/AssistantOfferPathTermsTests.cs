using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantOfferPathTermsTests
    {
        private static readonly DateTime Utc2026 = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Parse_CanonicalTwentyFivePercent_IsComplete()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid 30 days after issue",
                Utc2026
            );

            Assert.True(AssistantOfferPathTerms.IsComplete(state));
            Assert.Equal("percentage_discount", state.OfferType);
            Assert.Equal(25m, state.DiscountPercentage);
            Assert.Equal("30_days_after_issue", state.Validity);
            AssistantOfferPathTerms.ProposeCopy(state);
            Assert.Equal("25% off your next visit", state.Title);
        }

        [Fact]
        public void Parse_YearEnd_Uses31DecemberOfUtcYear()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer valid until the end of the year",
                Utc2026
            );

            Assert.Equal("choose_expiry_date", state.Validity);
            Assert.Equal("2026-12-31", state.ExpiryDate);
        }

        [Fact]
        public void Parse_OmitsValidity_DoesNotDefault()
        {
            var state = AssistantOfferPathTerms.Parse("Create a 25% Offer", Utc2026);

            Assert.Null(state.Validity);
            Assert.Contains("validity", AssistantOfferPathTerms.MissingFields(state));
        }

        [Fact]
        public void Parse_ConflictingBenefits_DoesNotPickOne()
        {
            var state = AssistantOfferPathTerms.Parse(
                "Create a 25% Offer and a free dessert",
                Utc2026
            );

            Assert.False(AssistantOfferPathTerms.IsComplete(state));
            Assert.Null(state.OfferType);
            Assert.True(state.ConflictingBenefits.Count >= 2);
            Assert.Contains(
                "authorised benefit",
                AssistantOfferPathTerms.GapBody(state),
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
