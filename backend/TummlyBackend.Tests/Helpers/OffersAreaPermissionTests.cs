using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class OffersAreaPermissionTests
    {
        [Theory]
        [InlineData("/api/offers/redeem/check", "POST", PermissionLevel.Scoped)]
        [InlineData("/api/offers/redeem", "POST", PermissionLevel.Scoped)]
        [InlineData("/api/offers", "GET", PermissionLevel.View)]
        [InlineData("/api/offers/12", "GET", PermissionLevel.View)]
        [InlineData("/api/offers", "POST", PermissionLevel.Manage)]
        [InlineData("/api/offers/12", "PUT", PermissionLevel.Manage)]
        [InlineData("/api/offers/12/pause", "POST", PermissionLevel.Manage)]
        [InlineData("/api/offers/12/archive", "POST", PermissionLevel.Manage)]
        [InlineData("/api/offers/12/duplicate", "POST", PermissionLevel.Manage)]
        public void MinimumForHttpRequest_MapsPathAndMethod(
            string path,
            string method,
            PermissionLevel expected
        )
        {
            Assert.Equal(
                expected,
                OffersAreaPermission.MinimumForHttpRequest(path, method)
            );
        }
    }
}
