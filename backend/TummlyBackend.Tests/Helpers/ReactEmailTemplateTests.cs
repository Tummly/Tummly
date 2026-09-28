using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Tests.Helpers
{
    public class ReactEmailTemplateTests
    {
        [Fact]
        public void Otp_Generate_FillsHeadlineWithRestaurantName()
        {
            var html = OtpEmailTemplate.Generate(
                Env(),
                otp: "482913",
                restaurantName: "Mehmet's Grill",
                helpCentreUrl: "https://app.tummly.test/help-center",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Equal(
                "Verify your email to join Mehmet's Grill on Tummly",
                OtpEmailTemplate.Subject("Mehmet's Grill")
            );
            Assert.Contains("Verify your email to join Mehmet&#39;s Grill on Tummly", html);
            Assert.Contains("482913", html);
            Assert.Contains("This code expires in 10 minutes.", html);
            Assert.Contains(
                "src=\"https://app.tummly.test/email/tummly-logo-dark.png\"",
                html
            );
            Assert.DoesNotContain("{{", html);
            Assert.DoesNotContain("background-color:#141414", html);
        }

        [Fact]
        public void Otp_Generate_UsesFallbackHeadline_WhenRestaurantMissing()
        {
            Assert.Equal(
                "Your Tummly verification code",
                OtpEmailTemplate.Subject(null)
            );

            var html = OtpEmailTemplate.Generate(
                Env(),
                otp: "111222",
                restaurantName: null,
                helpCentreUrl: "https://app.tummly.test/help-center",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Your Tummly verification code", html);
            Assert.Contains("111222", html);
        }

        [Fact]
        public void ContactEnquiry_Generate_FillsTopicAndReference()
        {
            Assert.Equal("TUM-000042", ContactEnquiryReceivedEmailTemplate.FormatReference(42));
            Assert.Equal(
                "We've received your Tummly enquiry — TUM-000042",
                ContactEnquiryReceivedEmailTemplate.Subject("TUM-000042")
            );

            var html = ContactEnquiryReceivedEmailTemplate.Generate(
                Env(),
                topic: "Plans & pricing",
                reference: "TUM-000042",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("We&#x27;ve received your Tummly enquiry", html);
            Assert.Contains("Plans &amp; pricing", html);
            Assert.Contains("Reference:", html);
            Assert.Contains("TUM-000042", html);
            Assert.Contains("Privacy Notice", html);
            Assert.Contains("Company details", html);
            Assert.Contains("Turn everyday orders and visits", html);
            Assert.DoesNotContain("{{topic}}", html);
            Assert.DoesNotContain("{{reference}}", html);
        }

        [Fact]
        public void NewDeviceSignIn_Generate_FillsDetailsAndLinks()
        {
            var html = NewDeviceSignInEmailTemplate.Generate(
                Env(),
                firstName: "Alex",
                signInTime: "28 Sep 2026, 15:42 BST",
                deviceSummary: "Chrome on macOS",
                locationSummary: "London, United Kingdom",
                resetPasswordUrl: "https://app.tummly.test/forgot-password",
                helpCentreUrl: "https://app.tummly.test/help-center",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains(NewDeviceSignInEmailTemplate.Subject, html);
            Assert.Contains("Hi", html);
            Assert.Contains("Alex", html);
            Assert.Contains("28 Sep 2026, 15:42 BST", html);
            Assert.Contains("Chrome on macOS", html);
            Assert.Contains("London, United Kingdom", html);
            Assert.Contains(
                "href=\"https://app.tummly.test/forgot-password\"",
                html
            );
            Assert.Contains("We noticed a sign-in from a new device.", html);
            Assert.DoesNotContain("{{first_name}}", html);
            Assert.DoesNotContain("{{sign_in_time}}", html);
        }

        [Fact]
        public void ResetPassword_Generate_FillsResetLinkAndLegalFooter()
        {
            var html = ResetPasswordEmailTemplate.Generate(
                Env(),
                resetUrl: "https://app.tummly.test/reset-password?token=a&b=1",
                helpCentreUrl: "https://app.tummly.test/help-center",
                termsUrl: "https://app.tummly.test/terms",
                privacyUrl: "https://app.tummly.test/privacy",
                cookiePolicyUrl: "https://app.tummly.test/cookie-policy",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains(ResetPasswordEmailTemplate.Subject, html);
            Assert.Contains("Reset password", html);
            Assert.Contains(
                "href=\"https://app.tummly.test/reset-password?token=a&amp;b=1\"",
                html
            );
            Assert.Contains("This link expires in 30 minutes.", html);
            Assert.Contains("Do not reply to this automated email.", html);
            Assert.Contains("©", html);
            Assert.Contains("2026", html);
            Assert.Contains("Tummly", html);
            Assert.Contains("Cookie settings", html);
            Assert.Contains("Use this secure link to create a new password.", html);
            Assert.DoesNotContain("{{reset_url}}", html);
        }

        [Fact]
        public void PasswordChanged_Generate_FillsNameAndSupportLink()
        {
            var html = PasswordChangedEmailTemplate.Generate(
                Env(),
                firstName: "Alex & Co",
                helpCentreUrl: "https://app.tummly.test/help-center",
                termsUrl: "https://app.tummly.test/terms",
                privacyUrl: "https://app.tummly.test/privacy",
                cookiePolicyUrl: "https://app.tummly.test/cookie-policy",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains(PasswordChangedEmailTemplate.Subject, html);
            Assert.Contains("Hi", html);
            Assert.Contains("Alex &amp; Co", html);
            Assert.Contains("No action is needed if you made this change.", html);
            Assert.Contains("mailto:support@tummly.com", html);
            Assert.Contains("©", html);
            Assert.Contains("2026", html);
            Assert.DoesNotContain("{{first_name}}", html);
        }

        [Fact]
        public void PilotStarted_Generate_FillsNameRestaurantAndDate()
        {
            var html = PilotStartedEmailTemplate.Generate(
                Env(),
                firstName: "Alex",
                restaurantName: "Mehmet's Grill",
                pilotEndDate: "28 Oct 2026",
                dashboardUrl: "https://app.tummly.test",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Equal(PilotStartedEmailTemplate.Subject, PilotStartedEmailTemplate.Subject);
            Assert.Contains("Your 30-day Tummly Pilot has started", html);
            Assert.Contains("Alex", html);
            Assert.Contains("Mehmet&#39;s Grill", html);
            Assert.Contains("28 Oct 2026", html);
            Assert.Contains("Go to Tummly", html);
            Assert.DoesNotContain("{{first_name}}", html);
        }

        [Fact]
        public void PilotEndingSoon_Generate_FillsDaysRemaining()
        {
            Assert.Equal(
                "Your Tummly Pilot ends in 5 days",
                PilotEndingSoonEmailTemplate.Subject(5)
            );

            var html = PilotEndingSoonEmailTemplate.Generate(
                Env(),
                daysRemaining: "5",
                firstName: "Alex",
                restaurantName: "Mehmet's Grill",
                pilotEndDate: "28 Oct 2026",
                plansUrl: "https://app.tummly.test/plans",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Your Tummly Pilot ends in 5 days", html);
            Assert.Contains("View plans", html);
            Assert.DoesNotContain("{{days_remaining}}", html);
        }

        [Fact]
        public void PilotEnded_Generate_FillsRestaurantAndPlansCta()
        {
            var html = PilotEndedEmailTemplate.Generate(
                Env(),
                firstName: "Alex",
                restaurantName: "Mehmet's Grill",
                plansUrl: "https://app.tummly.test/plans",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains(PilotEndedEmailTemplate.Subject, html);
            Assert.Contains("Choose a plan", html);
            Assert.DoesNotContain("{{restaurant_name}}", html);
        }

        [Fact]
        public void PaymentConfirmed_Generate_FillsOrderDetails()
        {
            var html = PaymentConfirmedEmailTemplate.Generate(
                Env(),
                firstName: "Alex",
                orderDescription: "Growth — Monthly",
                amount: "£79.00",
                paymentDate: "28 Sep 2026",
                reference: "TM-000142",
                billingUrl: "https://app.tummly.test/billing",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Equal(
                PaymentConfirmedEmailTemplate.Subject,
                "Payment confirmed — Tummly"
            );
            Assert.Contains("Your payment has been confirmed.", html);
            Assert.Contains("Growth — Monthly", html);
            Assert.Contains("79.00", html);
            Assert.Contains("TM-000142", html);
            Assert.Contains("View billing", html);
            Assert.DoesNotContain("{{reference}}", html);
        }

        [Fact]
        public void ShopOrderConfirmed_Generate_FillsOrderDetails()
        {
            Assert.Equal(
                "Your Tummly order is confirmed",
                ShopOrderConfirmedEmailTemplate.Subject
            );

            var html = ShopOrderConfirmedEmailTemplate.Generate(
                Env(),
                firstName: "Alex",
                locationName: "Camden High Street",
                orderNumber: "ORD-1042",
                materialsLinesHtml: "Table Tent QR × 2<br />Window Sticker QR × 1",
                deliveryAddressHtml: "12 High Street<br />NW1 8AB",
                orderUrl: "https://app.tummly.test/multi-dashboard/shop?location=12&view=orders&shopOrderId=11111111-1111-1111-1111-111111111111",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Your Tummly order is confirmed", html);
            Assert.Contains("Alex", html);
            Assert.Contains("Camden High Street", html);
            Assert.Contains("ORD-1042", html);
            Assert.Contains("Table Tent QR × 2", html);
            Assert.Contains("12 High Street", html);
            Assert.Contains("View order", html);
            Assert.Contains("shopOrderId=11111111-1111-1111-1111-111111111111", html);
            Assert.DoesNotContain("{{order_number}}", html);
            Assert.DoesNotContain("{{materials_lines_html}}", html);
        }

        [Fact]
        public void ShopOrderDispatched_Generate_FillsTrackingAndEstimate()
        {
            Assert.Equal(
                "Your Tummly order is on its way",
                ShopOrderDispatchedEmailTemplate.Subject
            );

            var html = ShopOrderDispatchedEmailTemplate.Generate(
                Env(),
                firstName: "Alex",
                locationName: "Camden High Street",
                orderNumber: "ORD-1042",
                deliveryEstimate: "Typically 5–7 working days",
                trackingDetails: "https://track.example/abc",
                orderUrl: "https://app.tummly.test/single-dashboard/shop?location=3&view=orders&shopOrderId=22222222-2222-2222-2222-222222222222",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Your Tummly order is on its way", html);
            Assert.Contains("has been dispatched", html);
            Assert.Contains("ORD-1042", html);
            Assert.Contains("Typically 5–7 working days", html);
            Assert.Contains("https://track.example/abc", html);
            Assert.Contains("Track / View order", html);
            Assert.DoesNotContain("{{delivery_estimate}}", html);
            Assert.DoesNotContain("{{tracking_details}}", html);
        }

        [Fact]
        public void PaymentActionRequired_Generate_FillsOrderAndCta()
        {
            var html = PaymentActionRequiredEmailTemplate.Generate(
                Env(),
                firstName: "Alex",
                orderDescription: "Growth — Monthly",
                billingUrl: "https://app.tummly.test/billing",
                ctaLabel: "Review billing",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains(PaymentActionRequiredEmailTemplate.Subject, html);
            Assert.Contains("Growth — Monthly", html);
            Assert.Contains("Review billing", html);
            Assert.DoesNotContain("{{order_description}}", html);
        }

        [Fact]
        public void UsageWarning_Generate_FillsAllowanceStats()
        {
            Assert.Equal(
                "You're nearing your Email allowance",
                UsageWarningEmailTemplate.Subject("Email")
            );

            var html = UsageWarningEmailTemplate.Generate(
                Env(),
                allowanceKind: "Email",
                firstName: "Alex",
                percentUsed: "80%",
                usedAmount: "800",
                remainingAmount: "200",
                resetDate: "1 Oct 2026",
                usageUrl: "https://app.tummly.test/usage",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("You&#x27;re nearing your Email allowance", html);
            Assert.Contains("80%", html);
            Assert.Contains("View usage", html);
            Assert.DoesNotContain("{{percent_used}}", html);
        }

        [Fact]
        public void UsageExhausted_Generate_FillsAllowanceAndCta()
        {
            Assert.Equal(
                "Your SMS allowance has been used",
                UsageExhaustedEmailTemplate.Subject("SMS")
            );

            var html = UsageExhaustedEmailTemplate.Generate(
                Env(),
                allowanceKind: "SMS",
                firstName: "Alex",
                ctaUrl: "https://app.tummly.test/credits",
                ctaLabel: "Add credits",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Your SMS allowance has been used", html);
            Assert.Contains("Add credits", html);
            Assert.DoesNotContain("{{allowance_kind}}", html);
        }

        [Fact]
        public void WeeklyBrief_Generate_FillsMetricsSummaryAndCta()
        {
            Assert.Equal(
                "Your Tummly Weekly Brief — Harbour Kitchen",
                WeeklyBriefEmailTemplate.Subject("Harbour Kitchen")
            );

            var html = WeeklyBriefEmailTemplate.Generate(
                Env(),
                firstName: "Alex",
                locationName: "Harbour Kitchen",
                periodLabel: "6–12 July",
                qrScans: 142,
                feedbackReceived: 38,
                guestsCaptured: 27,
                offerClaimed: 19,
                redemptions: 11,
                campaignEngagement: 64,
                whatChanged: "QR scans rose versus last week.",
                recommendedNextStep: "Follow up with guests who left contact details.",
                weeklyBriefUrl:
                    "https://app.tummly.test/single-dashboard/reports/weekly-brief?location=1",
                privacyUrl: "https://app.tummly.test/privacy",
                companyDetailsUrl: "https://app.tummly.test/terms",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Your Tummly Weekly Brief", html);
            Assert.Contains("Harbour Kitchen", html);
            Assert.Contains("6–12 July", html);
            Assert.Contains("142", html);
            Assert.Contains("QR scans", html);
            Assert.Contains("Feedback received", html);
            Assert.Contains("Guests captured", html);
            Assert.Contains("Offer claimed", html);
            Assert.Contains("Redemptions", html);
            Assert.Contains("Campaign engagement", html);
            Assert.Contains("What changed", html);
            Assert.Contains("QR scans rose versus last week.", html);
            Assert.Contains("Recommended next step", html);
            Assert.Contains(
                "Follow up with guests who left contact details.",
                html
            );
            Assert.Contains("View full Weekly Brief", html);
            Assert.Contains(
                "href=\"https://app.tummly.test/single-dashboard/reports/weekly-brief?location=1\"",
                html
            );
            Assert.DoesNotContain("{{first_name}}", html);
            Assert.DoesNotContain("{{qr_scans}}", html);
            Assert.DoesNotContain("{{what_changed}}", html);
        }

        private static StubWebHostEnvironment Env()
        {
            var contentRoot = FindBackendContentRoot();
            return new StubWebHostEnvironment { ContentRootPath = contentRoot };
        }

        private static string FindBackendContentRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(
                    dir.FullName,
                    "Assets",
                    "emails",
                    "templates",
                    "otp.html"
                );
                if (File.Exists(candidate))
                {
                    return dir.FullName;
                }

                var nested = Path.Combine(
                    dir.FullName,
                    "TummlyBackend",
                    "Assets",
                    "emails",
                    "templates",
                    "otp.html"
                );
                if (File.Exists(nested))
                {
                    return Path.Combine(dir.FullName, "TummlyBackend");
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate Assets/emails/templates/otp.html for tests."
            );
        }

        private sealed class StubWebHostEnvironment : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Test";
            public string ApplicationName { get; set; } = "Tests";
            public string WebRootPath { get; set; } = ".";
            public string ContentRootPath { get; set; } = ".";
            public IFileProvider WebRootFileProvider { get; set; } =
                new NullFileProvider();
            public IFileProvider ContentRootFileProvider { get; set; } =
                new NullFileProvider();
        }
    }
}
