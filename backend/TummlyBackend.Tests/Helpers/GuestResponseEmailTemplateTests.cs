using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Tests.Helpers
{
    public class GuestResponseEmailTemplateTests
    {
        [Fact]
        public void Generate_IncludesVenueBrandMessageAndFooter_WithoutOfferBlock()
        {
            var html = GuestResponseEmailTemplate.Generate(
                brandTitle: "Burger House",
                brandSubtitle: "Camden High Street",
                locationAddress: "12 High Street, London",
                subject: "Thanks for your visit",
                message: "Hi Sarah,\n\nThanks for visiting Burger House.",
                giveFeedbackUrl: "https://app.tummly.test/g/abc",
                frontendBaseUrl: "https://app.tummly.test",
                tummlyLogoDataUri: "data:image/png;base64,AAA",
                brandLogoUrl: null
            );

            Assert.Contains("Burger House", html);
            Assert.Contains("Camden High Street", html);
            Assert.Contains("Thanks for your visit", html);
            Assert.Contains("Hi Sarah,", html);
            Assert.Contains("Thanks for visiting Burger House.", html);
            Assert.Contains("Give feedback", html);
            Assert.Contains("https://app.tummly.test/g/abc", html);
            Assert.Contains(
                "You&#39;re receiving this because you joined Burger House guests list",
                html
            );
            Assert.Contains("Burger House, 12 High Street, London", html);
            Assert.Contains("Unsubscribe", html);
            Assert.Contains("Terms", html);
            Assert.Contains("Privacy", html);
            Assert.Contains("Cookie settings", html);
            Assert.Contains("Powered by", html);
            Assert.Contains("data:image/png;base64,AAA", html);
            Assert.DoesNotContain("Expires:", html);
            Assert.DoesNotContain("Copy", html);
        }

        [Fact]
        public void Generate_HtmlEncodesUserContent()
        {
            var html = GuestResponseEmailTemplate.Generate(
                brandTitle: "<Brand>",
                brandSubtitle: null,
                locationAddress: "A & B",
                subject: "Sub <script>",
                message: "Hello <b>x</b>",
                giveFeedbackUrl: "https://app.tummly.test/",
                frontendBaseUrl: "https://app.tummly.test",
                tummlyLogoDataUri: "data:image/png;base64,AAA",
                brandLogoUrl: null
            );

            Assert.Contains("&lt;Brand&gt;", html);
            Assert.Contains("A &amp; B", html);
            Assert.Contains("Sub &lt;script&gt;", html);
            Assert.Contains("Hello &lt;b&gt;x&lt;/b&gt;", html);
            Assert.DoesNotContain("<script>", html);
            Assert.DoesNotContain("<b>x</b>", html);
        }

        [Fact]
        public void Generate_OmitsSubjectAndSubtitleWhenMissing()
        {
            var html = GuestResponseEmailTemplate.Generate(
                brandTitle: "Solo Venue",
                brandSubtitle: null,
                locationAddress: null,
                subject: "   ",
                message: "Body only",
                giveFeedbackUrl: "https://app.tummly.test/",
                frontendBaseUrl: "https://app.tummly.test",
                tummlyLogoDataUri: "data:image/png;base64,AAA",
                brandLogoUrl: null
            );

            Assert.Contains("Solo Venue", html);
            Assert.Contains("Body only", html);
            Assert.DoesNotContain("data-guest-response-subject", html);
            Assert.Contains("Solo Venue, —", html);
        }
    }
}
