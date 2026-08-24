using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class CampaignSenderDisplayNameTests
    {
        [Fact]
        public void Resolve_UsesDefaultCampaignSenderName_WhenSet()
        {
            var restaurant = new Restaurant
            {
                Name = "Workspace Name",
                DefaultCampaignSenderName = "  Harbour Kitchen  ",
            };

            Assert.Equal(
                "Harbour Kitchen",
                CampaignSenderDisplayName.Resolve(restaurant, "Main Street")
            );
        }

        [Fact]
        public void Resolve_FallsBackToWorkspaceName_WhenSenderUnset()
        {
            var restaurant = new Restaurant
            {
                Name = "Workspace Name",
                DefaultCampaignSenderName = "   ",
            };

            Assert.Equal(
                "Workspace Name",
                CampaignSenderDisplayName.Resolve(restaurant, "Main Street")
            );
        }

        [Fact]
        public void Resolve_FallsBackToLocationName_WhenWorkspaceNameEmpty()
        {
            var restaurant = new Restaurant
            {
                Name = "  ",
                DefaultCampaignSenderName = null,
            };

            Assert.Equal(
                "Main Street",
                CampaignSenderDisplayName.Resolve(restaurant, "Main Street")
            );
        }

        [Fact]
        public void Resolve_UsesLocationName_WhenRestaurantNull()
        {
            Assert.Equal(
                "Solo Location",
                CampaignSenderDisplayName.Resolve(null, "Solo Location")
            );
        }
    }
}
