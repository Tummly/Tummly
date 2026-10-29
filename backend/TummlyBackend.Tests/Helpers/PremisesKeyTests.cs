using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class PremisesKeyTests
    {
        [Fact]
        public void Build_ignores_case_whitespace_and_punctuation()
        {
            var left = PremisesKey.Build("10,  Wardour Street.", "London", "w1d6qf");
            var right = PremisesKey.Build("10 wardour street", "London", "W1D 6QF");

            Assert.NotNull(left);
            Assert.Equal(left, right);
        }

        [Theory]
        [InlineData("1 Curtain Rd", "1 Curtain Road")]
        [InlineData("12 Market St", "12 Market Street")]
        [InlineData("3 Fox Av", "3 Fox Avenue")]
        [InlineData("7 Park Ln", "7 Park Lane")]
        [InlineData("2 Oak Dr", "2 Oak Drive")]
        [InlineData("Apt 2, 5 Church St", "Apartment 2, 5 Church Street")]
        public void Build_treats_common_abbreviations_as_the_same_address(
            string abbreviated,
            string full
        )
        {
            Assert.Equal(
                PremisesKey.Build(abbreviated, "London", "EC2A 3NZ"),
                PremisesKey.Build(full, "London", "EC2A 3NZ")
            );
        }

        [Fact]
        public void Build_keeps_distinct_units_at_one_postcode_apart()
        {
            var unit3 = PremisesKey.Build("Unit 3, 10 Wardour Street", "London", "W1D 6QF");
            var unit4 = PremisesKey.Build("Unit 4, 10 Wardour Street", "London", "W1D 6QF");
            var noUnit = PremisesKey.Build("10 Wardour Street", "London", "W1D 6QF");

            Assert.NotEqual(unit3, unit4);
            Assert.NotEqual(unit3, noUnit);
        }

        [Theory]
        [InlineData("10 Wardour Street, London", "London", "W1D 6QF")]
        [InlineData("10 Wardour Street, London, W1D 6QF", "London", "W1D 6QF")]
        [InlineData("10 Wardour Street W1D 6QF", "London", "W1D 6QF")]
        public void Build_drops_city_and_postcode_repeated_at_the_end_of_the_address(
            string addressWithSuffix,
            string city,
            string postcode
        )
        {
            Assert.Equal(
                PremisesKey.Build("10 Wardour Street", city, postcode),
                PremisesKey.Build(addressWithSuffix, city, postcode)
            );
        }

        [Fact]
        public void Build_gives_different_keys_for_different_postcodes()
        {
            Assert.NotEqual(
                PremisesKey.Build("10 Wardour Street", "London", "W1D 6QF"),
                PremisesKey.Build("10 Wardour Street", "London", "W1D 6QG")
            );
        }

        [Fact]
        public void Build_returns_null_when_address_or_postcode_is_blank()
        {
            Assert.Null(PremisesKey.Build(" ", "London", "W1D 6QF"));
            Assert.Null(PremisesKey.Build("10 Wardour Street", "London", null));
            Assert.Null(PremisesKey.Build("10 Wardour Street", "London", "  "));
        }
    }
}
