using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Seam: <see cref="GuestPreferencesSessionFilter"/> — signing secret configuration
    /// errors are not reported as an expired or missing session.
    /// </summary>
    public class GuestPreferencesSessionFilterTests
    {
        [Fact]
        public async Task MissingSigningSecret_ThrowsConfigurationError_NotA401()
        {
            var sut = new GuestPreferencesSessionFilter(
                new ConfigurationBuilder().Build(),
                TimeProvider.System
            );
            var http = new DefaultHttpContext();
            http.Request.Headers[GuestPreferencesSessionToken.HeaderName] = "anything.at-all";
            var executing = new ActionExecutingContext(
                new ActionContext(http, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller: new object()
            );

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.OnActionExecutionAsync(executing, ActionNotReached)
            );
            Assert.Null(executing.Result);
        }

        // Distinct from the expected InvalidOperationException, so a run of the action fails the test.
        private static Task<ActionExecutedContext> ActionNotReached() =>
            throw new NotSupportedException("The action must not run without a signing secret.");
    }
}
