namespace TummlyBackend.Services
{
    public static class ActivationState
    {
        public static bool HasActivationState(ActivationSubject subject) =>
            !string.IsNullOrEmpty(subject.ActivationCodeHash)
            || subject.ActivatedAt != null;

        public static bool RequiresActivation(ActivationSubject subject) =>
            subject.ActivatedAt == null;

        public static bool IsPendingActivation(ActivationSubject subject) =>
            !string.IsNullOrEmpty(subject.ActivationCodeHash)
            && subject.ActivatedAt == null;

        public static bool IsWithinActivationPeriod(ActivationSubject subject) =>
            subject.ActivatedAt != null
            && subject.ActivationExpiresAt.HasValue
            && subject.ActivationExpiresAt.Value > DateTime.UtcNow;

        // ActivatedAt without ActivationExpiresAt is corrupt data — treat as
        // expired so a partial write cannot grant permanent access.
        public static bool IsActivationExpired(ActivationSubject subject) =>
            subject.ActivatedAt != null
            && (
                !subject.ActivationExpiresAt.HasValue
                || subject.ActivationExpiresAt.Value <= DateTime.UtcNow
            );

        public static string? GetStatusDetail(ActivationSubject? subject)
        {
            if (subject == null || !HasActivationState(subject))
            {
                return null;
            }

            if (IsPendingActivation(subject))
            {
                return "pending";
            }

            if (IsActivationExpired(subject))
            {
                return "expired";
            }

            if (IsWithinActivationPeriod(subject))
            {
                return "active";
            }

            return null;
        }
    }
}
