using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Shared assertions for Tummly Shop integration tests.
    /// </summary>
    public static class ShopTestAssert
    {
        /// <summary>
        /// Asserts a 409 response with the Guest Card card-offer error code.
        /// </summary>
        public static async Task AssertOfferCardOfferRequiredAsync(
            HttpResponseMessage response
        )
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                OfferCardOfferRequiredException.ErrorCode,
                body.GetProperty("code").GetString()
            );
        }
    }
}
