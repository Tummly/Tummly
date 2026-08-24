using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Fire gate: blocks outbound send while Restaurant WorkspaceStatus is
    /// Paused. Soft-lock / hard-stop remain clear until Billing APIs exist.
    /// </summary>
    public sealed class ClearCampaignSendStartGate : ICampaignSendStartGate
    {
        private readonly ApplicationDbContext _context;

        public ClearCampaignSendStartGate(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CampaignSendStartGateResult> EvaluateAsync(
            int campaignId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var workspaceStatus = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => (WorkspaceStatus?)l.Restaurant!.WorkspaceStatus)
                .FirstOrDefaultAsync(cancellationToken);

            if (workspaceStatus == WorkspaceStatus.Paused)
            {
                return new CampaignSendStartGateResult.Blocked
                {
                    Message = "Workspace is paused.",
                };
            }

            return new CampaignSendStartGateResult.Clear();
        }
    }
}
