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

        [Fact]
        public void GetById_ReturnsStaticPreviewFields_ForQuietTimeBoost()
        {
            var service = new CampaignTemplateCatalogueService();
            var detail = service.GetById("quiet-time-boost");

            Assert.NotNull(detail);
            Assert.NotNull(detail!.Preview);
            Assert.Equal(
                "Bring eligible guests back during a quiet day or time",
                detail.Preview.Summary.Goal
            );
            Assert.Equal(
                new[] { "email", "sms" },
                detail.Preview.SuggestedChannels
            );
            Assert.Equal(2, detail.Preview.Messages.Count);
            Assert.NotNull(detail.Preview.OfferLogic);
            Assert.Equal(16, detail.Preview.Eligibility.EmailCount);
            Assert.Equal(4, detail.Preview.Eligibility.SmsCount);
            Assert.Equal(20, detail.Preview.Eligibility.TotalUniqueGuests);
            Assert.Equal(
                "Send Monday 10am for Tuesday lunch.",
                detail.Preview.SuggestedTiming
            );
        }

        [Fact]
        public void GetById_HidesOfferLogic_WhenOfferIsOptional()
        {
            var service = new CampaignTemplateCatalogueService();
            var detail = service.GetById("thank-recent-guests");

            Assert.NotNull(detail);
            Assert.NotNull(detail!.Preview);
            Assert.Null(detail.Preview.OfferLogic);
            Assert.All(
                detail.Preview.Messages,
                message => Assert.Null(message.OfferBlock)
            );
        }

        [Fact]
        public void List_DoesNotExposePreviewOnListItems()
        {
            var service = new CampaignTemplateCatalogueService();
            var response = service.List();
            var listItemType = response.Items[0].GetType();
            Assert.Null(listItemType.GetProperty("Preview"));
        }
    }
}
