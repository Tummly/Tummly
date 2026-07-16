using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace TummlyBackend.Hubs
{
    /// <summary>
    /// Shared JWT SignalR hub registry for operator realtime surfaces (ADR-0009).
    /// Hubs stay separate; path allowlist and MapHub options stay in sync here.
    /// </summary>
    public static class OperatorSignalRHubs
    {
        public const string NotificationsPath = "/hubs/notifications";

        public const string FeedbackHomePath = "/hubs/feedback-home";

        public static readonly string[] Paths =
        [
            NotificationsPath,
            FeedbackHomePath,
        ];

        public static bool IsHubPath(PathString path)
        {
            foreach (var hubPath in Paths)
            {
                if (path.StartsWithSegments(hubPath))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// SignalR JS clients send JWT via accessTokenFactory → query access_token.
        /// </summary>
        public static void TryAssignAccessTokenFromQuery(
            HttpRequest request,
            Action<string> assignToken
        )
        {
            var accessToken = request.Query["access_token"].ToString();
            if (
                string.IsNullOrEmpty(accessToken)
                || !IsHubPath(request.Path)
            )
            {
                return;
            }

            assignToken(accessToken);
        }

        public static void EnsureRegisteredPath(string hubPath)
        {
            if (!Paths.Contains(hubPath, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"Hub path '{hubPath}' is not registered in {nameof(OperatorSignalRHubs)}.{nameof(Paths)}.",
                    nameof(hubPath)
                );
            }
        }

        public static HubEndpointConventionBuilder MapOperatorHub<THub>(
            this IEndpointRouteBuilder endpoints,
            string hubPath
        )
            where THub : Hub
        {
            EnsureRegisteredPath(hubPath);

            return endpoints.MapHub<THub>(
                hubPath,
                options => options.CloseOnAuthenticationExpiration = true
            );
        }
    }
}
