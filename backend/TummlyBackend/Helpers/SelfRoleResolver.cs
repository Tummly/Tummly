using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Resolves Self role from the operator's linked Trial Request
    /// (distinct from permission <c>User.Role</c>).
    /// </summary>
    public static class SelfRoleResolver
    {
        public static string? Resolve(
            string userEmail,
            IEnumerable<TrialRequest> trialRequests
        )
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return null;
            }

            var match = trialRequests
                .Where(trial =>
                    trial.IsAccountCreated
                    && string.Equals(
                        trial.Email,
                        userEmail,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .OrderByDescending(trial =>
                    trial.AccountCreatedAt ?? trial.CreatedAt
                )
                .FirstOrDefault();

            if (match == null || string.IsNullOrWhiteSpace(match.Role))
            {
                return null;
            }

            return match.Role.Trim();
        }
    }
}
