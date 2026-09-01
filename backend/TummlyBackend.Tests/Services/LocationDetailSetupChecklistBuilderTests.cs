using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class LocationDetailSetupChecklistBuilderTests
    {
        [Fact]
        public void Build_ActiveLocationWithFacts_MarksCompleteRows()
        {
            var checklist = LocationDetailSetupChecklistBuilder.Build(
                LocationLifecycleStatus.Active,
                name: "Venue",
                address: "1 Street",
                city: "Camden",
                postcode: "NW1 1AA",
                hasActiveQr: true,
                anyQrCount: 2,
                privacyReady: true,
                managerUserId: 7,
                hasOffer: true
            );

            Assert.Equal("complete", checklist["locationDetailsAdded"]);
            Assert.Equal("complete", checklist["qrCodePublishedLive"]);
            Assert.Equal("complete", checklist["guestFormConnected"]);
            Assert.Equal("complete", checklist["teamAccessAssigned"]);
            Assert.Equal("complete", checklist["guestPrivacyNotice"]);
            Assert.Equal("complete", checklist["firstOfferCreated"]);
            Assert.Equal("complete", checklist["atLeastOneQrCreated"]);
        }

        [Fact]
        public void Build_DraftLocation_UsesNotStartedAndOptional()
        {
            var checklist = LocationDetailSetupChecklistBuilder.Build(
                LocationLifecycleStatus.Draft,
                name: "Draft",
                address: "1 Street",
                city: "Soho",
                postcode: "W1D 1AA",
                hasActiveQr: false,
                anyQrCount: 0,
                privacyReady: false,
                managerUserId: null,
                hasOffer: false
            );

            Assert.Equal("not-started", checklist["locationDetailsAdded"]);
            Assert.Equal("not-started", checklist["qrCodePublishedLive"]);
            Assert.Equal("optional", checklist["teamAccessAssigned"]);
            Assert.Equal("optional", checklist["firstOfferCreated"]);
        }

        [Fact]
        public void Build_ActiveMissingAddress_MarksLocationDetailsIncomplete()
        {
            var checklist = LocationDetailSetupChecklistBuilder.Build(
                LocationLifecycleStatus.Active,
                name: "Venue",
                address: "",
                city: "Camden",
                postcode: "NW1 1AA",
                hasActiveQr: false,
                anyQrCount: 0,
                privacyReady: true,
                managerUserId: null,
                hasOffer: false
            );

            Assert.Equal("incomplete", checklist["locationDetailsAdded"]);
            Assert.Equal("incomplete", checklist["qrCodePublishedLive"]);
        }
    }
}
