using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Http;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Public product-demo sessions consume no credits (ENG07): completed AI
    /// answers and drafts succeed without a ledger write, so one visitor cannot
    /// drain the shared demo pool. All other ledger calls pass through.
    /// </summary>
    public class DemoSessionCreditLedger : DispatchProxy
    {
        private ICreditLedger _inner = null!;
        private IHttpContextAccessor _httpContextAccessor = null!;

        public static ICreditLedger Create(
            ICreditLedger inner,
            IHttpContextAccessor httpContextAccessor
        )
        {
            var proxy = Create<ICreditLedger, DemoSessionCreditLedger>();
            var ledger = (DemoSessionCreditLedger)(object)proxy;
            ledger._inner = inner;
            ledger._httpContextAccessor = httpContextAccessor;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (
                targetMethod.Name == nameof(ICreditLedger.ConsumeOnSuccessAsync)
                && DemoSessionClaims.IsDemoSession(_httpContextAccessor.HttpContext?.User)
            )
            {
                return Task.FromResult(new CreditLedgerWriteResult { Succeeded = true });
            }

            try
            {
                return targetMethod.Invoke(_inner, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
    }
}
