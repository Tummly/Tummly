using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Tests.Helpers
{
    public class EmailAssetsTests
    {
        [Fact]
        public void ResolveChromeBaseUrl_PrefersPublicApiOverFrontend()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["PublicApi:BaseUrl"] = "https://api.tummly.test/",
                        ["Frontend:BaseUrl"] = "https://app.tummly.test",
                    }
                )
                .Build();

            Assert.Equal(
                "https://api.tummly.test",
                EmailAssets.ResolveChromeBaseUrl(configuration)
            );
            Assert.Equal(
                "https://api.tummly.test/email/tummly-logo-dark.png",
                EmailAssets.GetDarkLogoPublicUrl(configuration)
            );
        }

        [Fact]
        public void ResolveChromeBaseUrl_FallsBackToFrontend()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.test/",
                    }
                )
                .Build();

            Assert.Equal(
                "https://app.tummly.test",
                EmailAssets.ResolveChromeBaseUrl(configuration)
            );
        }

        [Fact]
        public void IsLoopbackChromeBase_DetectsLocalHosts()
        {
            Assert.True(EmailAssets.IsLoopbackChromeBase("http://127.0.0.1:5204"));
            Assert.True(EmailAssets.IsLoopbackChromeBase("http://localhost:5173"));
            Assert.False(
                EmailAssets.IsLoopbackChromeBase("https://api.qa.tummly.com")
            );
        }

        [Fact]
        public void TryContentRootFilePath_AllowsOnlyPublicPngs()
        {
            var env = new StubWebHostEnvironment
            {
                ContentRootPath = FindBackendContentRoot(),
            };

            Assert.NotNull(
                EmailAssets.TryContentRootFilePath(env, "tummly-logo-dark.png")
            );
            Assert.Null(
                EmailAssets.TryContentRootFilePath(env, "templates/otp.html")
            );
            Assert.Null(EmailAssets.TryContentRootFilePath(env, "../Secrets.env"));
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
                    "tummly-logo-dark.png"
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
                    "tummly-logo-dark.png"
                );
                if (File.Exists(nested))
                {
                    return Path.Combine(dir.FullName, "TummlyBackend");
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate Assets/emails/tummly-logo-dark.png for tests."
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
