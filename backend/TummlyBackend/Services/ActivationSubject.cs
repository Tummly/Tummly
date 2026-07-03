using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed record ActivationSubject(
        DateTime? ActivatedAt,
        DateTime? ActivationExpiresAt,
        string? ActivationCodeHash
    )
    {
        public static ActivationSubject FromUser(User user) =>
            new(
                user.ActivatedAt,
                user.ActivationExpiresAt,
                user.ActivationCodeHash
            );
    }
}
