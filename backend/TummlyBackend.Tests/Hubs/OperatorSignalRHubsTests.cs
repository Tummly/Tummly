using Microsoft.AspNetCore.Http;
using TummlyBackend.Hubs;

namespace TummlyBackend.Tests.Hubs
{
    public sealed class OperatorSignalRHubsTests
    {
        [Theory]
        [InlineData("/hubs/notifications", true)]
        [InlineData("/hubs/notifications/negotiate", true)]
        [InlineData("/hubs/feedback-home", true)]
        [InlineData("/api/notifications", false)]
        [InlineData("/hubs/other", false)]
        public void IsHubPath_MatchesRegisteredOperatorHubs(
            string path,
            bool expected
        )
        {
            Assert.Equal(
                expected,
                OperatorSignalRHubs.IsHubPath(new PathString(path))
            );
        }

        [Fact]
        public void TryAssignAccessTokenFromQuery_SetsToken_OnHubPath()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = OperatorSignalRHubs.NotificationsPath;
            context.Request.QueryString = new QueryString("?access_token=abc");

            string? assigned = null;
            OperatorSignalRHubs.TryAssignAccessTokenFromQuery(
                context.Request,
                token => assigned = token
            );

            Assert.Equal("abc", assigned);
        }

        [Fact]
        public void TryAssignAccessTokenFromQuery_Ignores_NonHubPath()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/notifications";
            context.Request.QueryString = new QueryString("?access_token=abc");

            string? assigned = null;
            OperatorSignalRHubs.TryAssignAccessTokenFromQuery(
                context.Request,
                token => assigned = token
            );

            Assert.Null(assigned);
        }

        [Fact]
        public void EnsureRegisteredPath_RejectsPath_NotInRegistry()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                OperatorSignalRHubs.EnsureRegisteredPath("/hubs/not-registered")
            );

            Assert.Equal("hubPath", ex.ParamName);
        }

        [Fact]
        public void EnsureRegisteredPath_AcceptsRegisteredPaths()
        {
            OperatorSignalRHubs.EnsureRegisteredPath(
                OperatorSignalRHubs.NotificationsPath
            );
            OperatorSignalRHubs.EnsureRegisteredPath(
                OperatorSignalRHubs.FeedbackHomePath
            );
        }
    }
}
