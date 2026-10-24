using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Helpers;

namespace TummlyBackend.Infrastructure
{
    public static class DemoAwareAiProviderRegistration
    {
        /// <summary>
        /// Registers <typeparamref name="TService"/> as the live provider for
        /// operators and jobs, and the deterministic twin for public
        /// product-demo sessions.
        /// </summary>
        public static IServiceCollection AddDemoAwareAiProvider<
            TService,
            TLive,
            TDemo
        >(this IServiceCollection services)
            where TService : class
            where TLive : class, TService
            where TDemo : class, TService
        {
            services.AddSingleton<TLive>();
            services.AddSingleton<TDemo>();
            services.AddSingleton<TService>(sp =>
                DemoAwareAiProvider.Create<TService>(
                    sp.GetRequiredService<TLive>(),
                    sp.GetRequiredService<TDemo>(),
                    sp.GetRequiredService<IHttpContextAccessor>()
                )
            );
            return services;
        }
    }
}
