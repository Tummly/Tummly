using Microsoft.Extensions.Configuration;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class BrandLogoRulesTests
    {
        [Fact]
        public void BuildAbsolutePublicUrl_FromConfig_PrefersPublicApi()
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

            var url = BrandLogoRules.BuildAbsolutePublicUrl(
                "brand-logos/abc.png",
                configuration
            );

            Assert.Equal(
                "https://api.tummly.test/api/public/brand-logos/abc.png",
                url
            );
        }

        [Fact]
        public void BuildAbsolutePublicUrl_FromConfig_FallsBackToFrontend()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.test/",
                    }
                )
                .Build();

            var url = BrandLogoRules.BuildAbsolutePublicUrl(
                "brand-logos/abc.png",
                configuration
            );

            Assert.Equal(
                "https://app.tummly.test/api/public/brand-logos/abc.png",
                url
            );
        }

        [Fact]
        public void BuildAbsolutePublicUrl_FromConfig_ReturnsNull_WhenNoLogo()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["PublicApi:BaseUrl"] = "https://api.tummly.test",
                    }
                )
                .Build();

            Assert.Null(
                BrandLogoRules.BuildAbsolutePublicUrl(null, configuration)
            );
            Assert.Null(
                BrandLogoRules.BuildAbsolutePublicUrl("  ", configuration)
            );
        }
    }
}
