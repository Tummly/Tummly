using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Tests.Helpers
{
    public class TeamInvitationEmailTemplateTests
    {
        [Fact]
        public void Generate_FillsTokens_AndKeepsInvitationMessage()
        {
            var html = TeamInvitationEmailTemplate.Generate(
                Env(),
                greetingName: "Alex",
                inviterName: "Jordan Lee",
                workspaceName: "Mehmet's Grill",
                roleName: "Manager",
                locationScope: "All locations",
                invitationMessage: "Welcome aboard.",
                acceptUrl: "https://app.tummly.test/invite?t=1&x=2",
                helpCentreUrl: "https://app.tummly.test/help-center",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Hi Alex,", html);
            Assert.Contains("Jordan Lee", html);
            Assert.Contains("Mehmet&#39;s Grill", html);
            Assert.Contains("Role:", html);
            Assert.Contains("Manager", html);
            Assert.Contains("All locations", html);
            Assert.Contains("Welcome aboard.", html);
            Assert.Contains("data-slot=\"invitation-message\"", html);
            Assert.Contains(
                "href=\"https://app.tummly.test/invite?t=1&amp;x=2\"",
                html
            );
            Assert.Contains("https://app.tummly.test/help-center", html);
            Assert.Contains(
                "src=\"https://app.tummly.test/email/tummly-logo-dark.png\"",
                html
            );
            Assert.DoesNotContain("data:image", html);
            Assert.DoesNotContain("cid:", html);
            Assert.Contains("Accept invitation", html);
            Assert.Contains("Need help?", html);
            Assert.Contains("support@tummly.com", html);
            Assert.Contains("If you did not request access to Tummly", html);
            Assert.Contains("ignore this email.", html);
            Assert.DoesNotContain("{{", html);
            Assert.DoesNotContain("background-color:#141414", html);
        }

        [Fact]
        public void Generate_OmitsInvitationMessage_WhenEmpty()
        {
            var html = TeamInvitationEmailTemplate.Generate(
                Env(),
                greetingName: "",
                inviterName: "Jordan",
                workspaceName: "Demo",
                roleName: "Staff",
                locationScope: "One location",
                invitationMessage: null,
                acceptUrl: "https://app.tummly.test/invite",
                helpCentreUrl: "https://app.tummly.test/help-center",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Hi,", html);
            Assert.DoesNotContain("data-slot=\"invitation-message\"", html);
            Assert.DoesNotContain("{{invitation_message}}", html);
        }

        [Fact]
        public void Generate_HtmlEncodesUserContent()
        {
            var html = TeamInvitationEmailTemplate.Generate(
                Env(),
                greetingName: "<script>",
                inviterName: "A & B",
                workspaceName: "<Brand>",
                roleName: "R<script>",
                locationScope: "X & Y",
                invitationMessage: "Hi <b>there</b>",
                acceptUrl: "https://app.tummly.test/invite",
                helpCentreUrl: "https://app.tummly.test/help-center",
                logoUrl: "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Hi &lt;script&gt;,", html);
            Assert.Contains("A &amp; B", html);
            Assert.Contains("&lt;Brand&gt;", html);
            Assert.Contains("R&lt;script&gt;", html);
            Assert.Contains("X &amp; Y", html);
            Assert.Contains("Hi &lt;b&gt;there&lt;/b&gt;", html);
            Assert.DoesNotContain("<script>", html);
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
                    "team-invitation.html"
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
                    "team-invitation.html"
                );
                if (File.Exists(nested))
                {
                    return Path.Combine(dir.FullName, "TummlyBackend");
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate Assets/emails/templates/team-invitation.html for tests."
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
