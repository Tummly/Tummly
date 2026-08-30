using TummlyBackend.Billing;

namespace TummlyBackend.DTOs.TeamPermissions
{
    public sealed class TeamPermissionsWriteResult
    {
        public string? Error { get; init; }

        public string? Code { get; init; }

        public int? Cap { get; init; }

        public int? Current { get; init; }

        public static TeamPermissionsWriteResult Ok() => new();

        public static TeamPermissionsWriteResult Fail(string error) => new()
        {
            Error = error,
        };

        public static TeamPermissionsWriteResult FromCap(
            TeamMemberCapDecision decision
        )
        {
            if (decision.Unavailable)
            {
                return Fail(TeamMemberCapGate.UnavailableMessage);
            }

            if (!decision.AllowIncrement)
            {
                return new TeamPermissionsWriteResult
                {
                    Error = TeamMemberCapGate.CapReachedMessage,
                    Code = TeamMemberCapGate.CapReachedCode,
                    Cap = decision.Cap,
                    Current = decision.Current,
                };
            }

            return Ok();
        }
    }
}
