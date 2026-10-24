using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Public product-demo actions must not draw down the shared demo
    /// restaurant's credits (ENG07: no charges; tour keeps working).
    /// </summary>
    public class DemoSessionCreditLedgerTests
    {
        public class RecordingLedger : DispatchProxy
        {
            public List<string> Calls { get; } = [];

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                Calls.Add(targetMethod!.Name);
                return Task.FromResult(
                    new CreditLedgerWriteResult { Succeeded = true, Code = "inner" }
                );
            }
        }

        private static (ICreditLedger Ledger, RecordingLedger Inner) Create(ClaimsPrincipal user)
        {
            var inner = DispatchProxy.Create<ICreditLedger, RecordingLedger>();
            var recorder = (RecordingLedger)(object)inner;
            var accessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext { User = user },
            };
            return (DemoSessionCreditLedger.Create(inner, accessor), recorder);
        }

        private static readonly CreditLedgerConsumeRequest AiConsume = new()
        {
            RestaurantId = 1,
            Channel = "ai",
            Units = 1,
            LocationId = 2,
        };

        [Fact]
        public async Task DemoSession_Consume_SucceedsWithoutWriting()
        {
            var (ledger, inner) = Create(DemoPrincipal());

            var result = await ledger.ConsumeOnSuccessAsync(AiConsume);

            Assert.True(result.Succeeded);
            Assert.Empty(inner.Calls);
        }

        [Fact]
        public async Task OperatorSession_Consume_ReachesLedger()
        {
            var (ledger, inner) = Create(OperatorPrincipal());

            var result = await ledger.ConsumeOnSuccessAsync(AiConsume);

            Assert.Equal("inner", result.Code);
            Assert.Equal([nameof(ICreditLedger.ConsumeOnSuccessAsync)], inner.Calls);
        }

        [Fact]
        public async Task DemoSession_OtherLedgerMethods_StillReachLedger()
        {
            var (ledger, inner) = Create(DemoPrincipal());

            await ledger.ReserveAsync(new CreditLedgerReserveRequest());

            Assert.Equal([nameof(ICreditLedger.ReserveAsync)], inner.Calls);
        }

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
