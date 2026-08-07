using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class CampaignTemplateCatalogueServiceTests
    {
        [Fact]
        public void List_Throws_WhenCatalogueIsEmpty()
        {
            var service = new CampaignTemplateCatalogueService(
                Array.Empty<CampaignTemplateDetailDto>()
            );

            var ex = Assert.Throws<InvalidOperationException>(
                () => service.List()
            );
            Assert.Equal(
                "Campaign template catalogue is empty.",
                ex.Message
            );
        }

        [Fact]
        public void List_ReturnsSeededCampaignTemplates()
        {
            var service = new CampaignTemplateCatalogueService();
            var response = service.List();

            Assert.Equal(6, response.Items.Count);
            Assert.Equal("thank-recent-guests", response.Items[0].Id);
            Assert.Equal(1, response.Items[0].Version);
        }

        [Fact]
        public void GetById_ReturnsNull_WhenMissing()
        {
            var service = new CampaignTemplateCatalogueService();
            Assert.Null(service.GetById("missing"));
        }
    }
}
