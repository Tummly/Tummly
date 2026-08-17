using TummlyBackend.Helpers;
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
            Assert.DoesNotContain("Give feedback", html);
            Assert.DoesNotContain("https://app.tummly.test/g/abc", html);
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
            Assert.Contains("role='presentation'", html);
            Assert.Contains(
                $"https://app.tummly.test{BaseNonTransactionalEmailTemplate.PublicLogoPath}",
                html
            );
            Assert.Contains(
                $"https://app.tummly.test{BaseNonTransactionalEmailTemplate.PublicTopDecorationPath}",
                html
            );
            Assert.DoesNotContain("cid:", html);
            Assert.DoesNotContain("cid:bottom-strip", html);
            Assert.DoesNotContain("data-guest-response-footer-strip", html);
            Assert.Contains("data-guest-response-top-decoration='1'", html);
            Assert.Contains("width='560'", html);
            Assert.Contains("margin-top:-330px", html);
            Assert.Contains("border-radius:10px", html);
            Assert.Contains("max-width:440px", html);
            Assert.Contains("data-non-transactional-slot='brand'", html);
            Assert.Contains("data-non-transactional-slot='ticket'", html);
            Assert.Contains("data-non-transactional-slot='legal'", html);
            Assert.Contains("data-non-transactional-slot='poweredBy'", html);
            Assert.DoesNotContain("background-color:#14a74a", html);
            Assert.DoesNotContain("data:image", html);
            Assert.DoesNotContain("position:absolute", html);
            Assert.DoesNotContain("transform:rotate", html);
            Assert.DoesNotContain("data-guest-response-notch=", html);
            Assert.DoesNotContain(
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
                frontendBaseUrl: "https://app.tummly.test",
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
                frontendBaseUrl: "https://app.tummly.test",
                brandLogoUrl: null
            );

            Assert.Contains("Solo Venue", html);
            Assert.Contains("Body only", html);
            Assert.DoesNotContain("data-guest-response-subject", html);
            Assert.Contains("Solo Venue, —", html);
        }

        [Fact]
        public void Generate_IncludesOfferBlock_WithClaimQr_OmitsGiveFeedback()
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
            Assert.Contains("data-guest-response-offer-qr='1'", html);
            Assert.Contains("data-non-transactional-slot='offer'", html);
            Assert.Contains("data-guest-response-notch='1'", html);
            Assert.Contains("#2c2c2c", html);
            Assert.Contains(
                OfferClaimQr.ToPngDataUri("BURGERCO-4829"),
                html
            );
            Assert.DoesNotContain("cid:", html);
            Assert.DoesNotContain("Give feedback", html);
        }

        [Fact]
        public void Generate_OfferClaimQr_UsesDataUri_NotCid()
        {
            const string claimCode = "BURGERCO-4829";
            var html = GenerateSample(
                offer: new GuestResponseEmailOfferBlock(
                    Title: "15% off",
                    Description: "Helper",
                    RedemptionCode: claimCode,
                    ExpiryLabel: "Expires: 31 July 2026"
                )
            );

            Assert.Contains(OfferClaimQr.ToPngDataUri(claimCode), html);
            Assert.DoesNotContain("cid:", html);
            Assert.DoesNotContain(OfferClaimQr.ToPngDataUri("OTHER-CODE"), html);
        }

        [Fact]
        public void ToPngBytes_WritesPngSignature()
        {
            var bytes = OfferClaimQr.ToPngBytes("BURGERCO-4829");

            Assert.True(bytes.Length > 8);
            Assert.Equal(0x89, bytes[0]);
            Assert.Equal((byte)'P', bytes[1]);
            Assert.Equal((byte)'N', bytes[2]);
            Assert.Equal((byte)'G', bytes[3]);
        }

        [Fact]
        public void Generate_OmitsGiveFeedback_WhenOfferBlockAbsent()
        {
            var html = GenerateSample(offer: null);

            Assert.DoesNotContain("Give feedback", html);
            Assert.DoesNotContain("data-guest-response-offer=", html);
            Assert.DoesNotContain("data-guest-response-offer-qr", html);
            Assert.DoesNotContain("cid:", html);
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
                frontendBaseUrl: "https://app.tummly.test",
                brandLogoUrl: null,
                offer: new GuestResponseEmailOfferBlock(
                    Title: "10% <off>",
                    Description: "From A & B",
                    RedemptionCode: "CODE<script>",
                    ExpiryLabel: "Expires: <soon>"
                )
            );

            Assert.Contains("10% &lt;off&gt;", html);
            Assert.Contains("From A &amp; B", html);
            Assert.Contains("CODE&lt;script&gt;", html);
            Assert.Contains("Expires: &lt;soon&gt;", html);
            Assert.DoesNotContain("<script>", html);
            Assert.DoesNotContain("Give feedback", html);
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
                frontendBaseUrl: "https://app.tummly.test",
                brandLogoUrl: null,
                offer: offer
            );
        }
    }
}
