using FluentValidation.TestHelper;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Validators;

namespace TummlyBackend.Tests.Validators
{
    public class CompleteSetupDtoValidatorTests
    {
        private readonly CompleteSetupDtoValidator _validator =
            new();

        [Fact]
        public void Should_reject_invalid_location_postcode()
        {
            var dto = CreateValidDto();
            dto.Locations[0].Postcode = "not-a-postcode";

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor("Locations[0].Postcode")
                .WithErrorMessage("Please enter a valid UK postcode.");
        }

        [Fact]
        public void Should_accept_address_overridden_flag_without_extra_rules()
        {
            var dto = CreateValidDto();
            dto.Locations[0].AddressOverridden = true;

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }

        private static CompleteSetupDto CreateValidDto()
        {
            return new CompleteSetupDto
            {
                Token = "token",
                Password = "password1",
                ConfirmPassword = "password1",
                GroupName = "Group",
                BusinessCategory = "takeaway",
                PrimaryPhone = "07700900123",
                Locations =
                [
                    new CompleteSetupDto.LocationItem
                    {
                        LocationName = "Main",
                        Address = "1 High Street, Manchester",
                        Postcode = "M1 4AB",
                    },
                ],
            };
        }
    }
}
