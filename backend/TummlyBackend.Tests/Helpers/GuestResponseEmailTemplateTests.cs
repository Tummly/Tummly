using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Tests.Helpers
{
    public class GuestResponseEmailTemplateTests
    {
        [Fact]
        public void Generate_IncludesVenueBrandMessageAndFooter_WithoutOfferBlock()
        {
            var html = GenerateSample();

            Assert.Contains("Burger House", html);
            Assert.Contains("Camden High Street", html);
            Assert.Contains("Thanks for your visit", html);
            Assert.Contains("Hi Sarah,", html);
            Assert.Contains("Thanks for visiting Burger House.", html);
            Assert.Contains("Give feedback", html);
            Assert.Contains("https://app.tummly.test/g/abc", html);
            Assert.Contains(
                "You&#39;re receiving this because you joined Burger House customer club",
                html
            );
            Assert.Contains("Burger House, 12 High Street, London", html);
            Assert.Contains("Unsubscribe", html);
            Assert.Contains("Terms", html);
            Assert.Contains("Privacy", html);
            Assert.Contains("Cookie settings", html);
            Assert.Contains("Powered by", html);
            Assert.Contains("data:image/png;base64,AAA", html);
            Assert.Contains("data-guest-response-footer-strip='1'", html);
            Assert.Contains("data:image/png;base64,STRIP", html);
            Assert.Contains(
                "background-image:url(data:image/png;base64,STRIP)",
                html
            );
            Assert.DoesNotContain("url('data:image", html);
            Assert.Contains("data-guest-response-top-decoration='1'", html);
            Assert.Contains("data:image/png;base64,TOP", html);
            Assert.Contains("data-guest-response-notch='1'", html);
            Assert.Contains(
                "background-color:#14a247;color:#ffffff",
                html
            );
            Assert.DoesNotContain("background-color:#36b468", html);
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
                brandLogoUrl: null,
                topDecorationDataUri: "data:image/png;base64,TOP",
                bottomStripDataUri: "data:image/png;base64,STRIP"
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
                brandLogoUrl: null,
                topDecorationDataUri: "data:image/png;base64,TOP",
                bottomStripDataUri: "data:image/png;base64,STRIP"
            );

            Assert.Contains("Solo Venue", html);
            Assert.Contains("Body only", html);
            Assert.DoesNotContain("data-guest-response-subject", html);
            Assert.Contains("Solo Venue, —", html);
        }

        [Fact]
        public void Generate_IncludesOfferBlock_WithCode_WithoutQr()
        {
            var html = GenerateSample(
                offer: new GuestResponseEmailOfferBlock(
                    Title: "15% off your next order",
                    Description:
                        "Show this code to the team on your next visit. This offer is from Burger House and is subject to the terms below.",
                    RedemptionCode: "BURGERCO-4829",
                    ExpiryLabel: "Expires: 31 July 2026"
                )
            );

            Assert.Contains("data-guest-response-offer='1'", html);
            Assert.Contains("15% off your next order", html);
            Assert.Contains(
                "Show this code to the team on your next visit. This offer is from Burger House and is subject to the terms below.",
                html
            );
            Assert.Contains("BURGERCO-4829", html);
            Assert.Contains("Copy", html);
            Assert.Contains("Expires: 31 July 2026", html);
            Assert.Contains("Give feedback", html);
            Assert.DoesNotContain("data-guest-response-offer-qr", html);
        }

        [Fact]
        public void Generate_HtmlEncodesOfferBlockContent()
        {
            var html = GuestResponseEmailTemplate.Generate(
                brandTitle: "Brand",
                brandSubtitle: null,
                locationAddress: null,
                subject: "Subject",
                message: "Body",
                giveFeedbackUrl: "https://app.tummly.test/",
                frontendBaseUrl: "https://app.tummly.test",
                tummlyLogoDataUri: "data:image/png;base64,AAA",
                brandLogoUrl: null,
                offer: new GuestResponseEmailOfferBlock(
                    Title: "10% <off>",
                    Description: "From A & B",
                    RedemptionCode: "CODE<script>",
                    ExpiryLabel: "Expires: <soon>"
                ),
                topDecorationDataUri: "data:image/png;base64,TOP",
                bottomStripDataUri: "data:image/png;base64,STRIP"
            );

            Assert.Contains("10% &lt;off&gt;", html);
            Assert.Contains("From A &amp; B", html);
            Assert.Contains("CODE&lt;script&gt;", html);
            Assert.Contains("Expires: &lt;soon&gt;", html);
            Assert.DoesNotContain("<script>", html);
        }

        private static string GenerateSample(
            GuestResponseEmailOfferBlock? offer = null
        )
        {
            return GuestResponseEmailTemplate.Generate(
                brandTitle: "Burger House",
                brandSubtitle: "Camden High Street",
                locationAddress: "12 High Street, London",
                subject: "Thanks for your visit",
                message: "Hi Sarah,\n\nThanks for visiting Burger House.",
                giveFeedbackUrl: "https://app.tummly.test/g/abc",
                frontendBaseUrl: "https://app.tummly.test",
                tummlyLogoDataUri: "data:image/png;base64,AAA",
                brandLogoUrl: null,
                offer: offer,
                topDecorationDataUri: "data:image/png;base64,TOP",
                bottomStripDataUri: "data:image/png;base64,STRIP"
            );
        }
    }
}
