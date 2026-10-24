using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Http;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Routes one AI provider interface per call: public product-demo sessions
    /// use the deterministic twin (no live AI calls or charges, ENG07); every
    /// other request and background job uses the live provider.
    /// </summary>
    public class DemoAwareAiProvider : DispatchProxy
    {
        private object _live = null!;
        private object _demo = null!;
        private IHttpContextAccessor _httpContextAccessor = null!;

        public static T Create<T>(
            T live,
            T demo,
            IHttpContextAccessor httpContextAccessor
        )
            where T : class
        {
            var proxy = Create<T, DemoAwareAiProvider>();
            var router = (DemoAwareAiProvider)(object)proxy;
            router._live = live;
            router._demo = demo;
            router._httpContextAccessor = httpContextAccessor;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            var target = DemoSessionClaims.IsDemoSession(
                _httpContextAccessor.HttpContext?.User
            )
                ? _demo
                : _live;

            try
            {
                return targetMethod.Invoke(target, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
    }
}
