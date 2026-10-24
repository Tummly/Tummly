using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Public product-demo requests must never reach a live AI provider
    /// (ENG07: no real AI charges). Other requests keep the live provider.
    /// </summary>
    public class DemoAwareAiProviderTests
    {
        public interface IGreetingProvider
        {
            Task<string> GreetAsync(string name, CancellationToken cancellationToken = default);
        }

        private sealed class NamedProvider(string label) : IGreetingProvider
        {
            public int Calls { get; private set; }

            public Task<string> GreetAsync(string name, CancellationToken cancellationToken = default)
            {
                Calls++;
                return Task.FromResult($"{label}:{name}");
            }
        }

        private sealed class ThrowingProvider : IGreetingProvider
        {
            public Task<string> GreetAsync(string name, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("provider failed");
        }

        [Fact]
        public async Task DemoSessionRequest_UsesDeterministicProvider()
        {
            var live = new NamedProvider("live");
            var demo = new NamedProvider("demo");
            var provider = DemoAwareAiProvider.Create<IGreetingProvider>(
                live,
                demo,
                AccessorFor(DemoPrincipal())
            );

            var result = await provider.GreetAsync("Mavero");

            Assert.Equal("demo:Mavero", result);
            Assert.Equal(0, live.Calls);
        }

        [Fact]
        public async Task OperatorRequest_UsesLiveProvider()
        {
            var live = new NamedProvider("live");
            var demo = new NamedProvider("demo");
            var provider = DemoAwareAiProvider.Create<IGreetingProvider>(
                live,
                demo,
                AccessorFor(OperatorPrincipal())
            );

            var result = await provider.GreetAsync("Camden");

            Assert.Equal("live:Camden", result);
            Assert.Equal(0, demo.Calls);
        }

        [Fact]
        public async Task NoHttpContext_UsesLiveProvider()
        {
            var live = new NamedProvider("live");
            var provider = DemoAwareAiProvider.Create<IGreetingProvider>(
                live,
                new NamedProvider("demo"),
                new HttpContextAccessor()
            );

            var result = await provider.GreetAsync("job");

            Assert.Equal("live:job", result);
        }

        [Fact]
        public async Task ProviderException_IsNotWrapped()
        {
            var provider = DemoAwareAiProvider.Create<IGreetingProvider>(
                new ThrowingProvider(),
                new NamedProvider("demo"),
                AccessorFor(OperatorPrincipal())
            );

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GreetAsync("x")
            );
            Assert.Equal("provider failed", ex.Message);
        }

        private static IHttpContextAccessor AccessorFor(ClaimsPrincipal user)
            => new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } };

        private static ClaimsPrincipal DemoPrincipal()
            => new(
                new ClaimsIdentity(
                    [new Claim(DemoSessionClaims.ClaimType, DemoSessionClaims.ClaimValue)],
                    authenticationType: "Test"
                )
            );

        private static ClaimsPrincipal OperatorPrincipal()
            => new(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "42")],
                    authenticationType: "Test"
                )
            );
    }
}
