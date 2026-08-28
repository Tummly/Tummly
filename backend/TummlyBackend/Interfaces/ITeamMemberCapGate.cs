using TummlyBackend.Billing;

namespace TummlyBackend.Interfaces
{
    public interface ITeamMemberCapGate
    {
        Task<TeamMemberCapDecision> DenyIncrementAsync(int restaurantId);
    }
}
