using System.Net;
using Microsoft.AspNetCore.Http;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class SignupClientIpTests
    {
        private const string ForwardedFor = "X-Forwarded-For";

        [Fact]
        public void SignupClientIp_RightMostForwardedEntry()
        {
            var ctx = ContextWith("198.51.100.1, 203.0.113.9", "10.0.0.5");

            Assert.Equal("203.0.113.9", SignupClientIp.Resolve(ctx, 1));
        }

        [Fact]
        public void SignupClientIp_SpoofedLeftEntriesIgnored()
        {
            var ctx = ContextWith("6.6.6.6, 9.9.9.9 , 203.0.113.9", "10.0.0.5");

            Assert.Equal("203.0.113.9", SignupClientIp.Resolve(ctx, 1));
            Assert.Equal("9.9.9.9", SignupClientIp.Resolve(ctx, 2));
        }

        [Fact]
        public void SignupClientIp_NoHeader_UsesRemoteIp()
        {
            var ctx = ContextWith(forwardedFor: null, remoteIp: "198.51.100.7");

            Assert.Equal("198.51.100.7", SignupClientIp.Resolve(ctx, 1));
        }

        [Fact]
        public void SignupClientIp_FewerEntriesThanHops_UsesRemoteIp()
        {
            var ctx = ContextWith("203.0.113.9", "10.0.0.5");

            Assert.Equal("10.0.0.5", SignupClientIp.Resolve(ctx, 2));
        }

        [Fact]
        public void SignupClientIp_NoHeaderNoRemote_Unknown()
        {
            var ctx = ContextWith(forwardedFor: null, remoteIp: null);

            Assert.Equal("unknown", SignupClientIp.Resolve(ctx, 1));
        }

        [Fact]
        public void SignupClientIp_StripsPort_IPv4()
        {
            var ctx = ContextWith("198.51.100.1, 203.0.113.9:51234", "10.0.0.5");

            Assert.Equal("203.0.113.9", SignupClientIp.Resolve(ctx, 1));
        }

        [Fact]
        public void SignupClientIp_StripsPort_IPv6Bracketed()
        {
            var ctx = ContextWith("[2001:db8::1]:51234", "10.0.0.5");

            Assert.Equal("2001:db8::1", SignupClientIp.Resolve(ctx, 1));
        }

        [Fact]
        public void SignupClientIp_PlainIPv6_Unchanged()
        {
            var ctx = ContextWith("2001:db8::7", "10.0.0.5");

            Assert.Equal("2001:db8::7", SignupClientIp.Resolve(ctx, 1));
        }

        private static DefaultHttpContext ContextWith(string? forwardedFor, string? remoteIp)
        {
            var ctx = new DefaultHttpContext();
            if (forwardedFor is not null)
            {
                ctx.Request.Headers[ForwardedFor] = forwardedFor;
            }

            ctx.Connection.RemoteIpAddress = remoteIp is null ? null : IPAddress.Parse(remoteIp);
            return ctx;
        }
    }
}
