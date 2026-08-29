using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Fire gate: Pause workspace, then operator billing lock (ticket 33).
    /// Soft-lock / dormant / past-due day 7 / chargeback collapse to CannotStart at fire.
    /// </summary>
    public sealed class ClearCampaignSendStartGate : ICampaignSendStartGate
    {
        private readonly ApplicationDbContext _context;
        private readonly Func<DateTime> _utcNow;

        public ClearCampaignSendStartGate(
            ApplicationDbContext context,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<CampaignSendStartGateResult> EvaluateAsync(
            int campaignId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var row = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => new
                {
                    WorkspaceStatus = (WorkspaceStatus?)l.Restaurant!.WorkspaceStatus,
                    RestaurantId = l.RestaurantId,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (row == null)
            {
                return new CampaignSendStartGateResult.Blocked
                {
                    Message = "Campaign location was not found.",
                };
            }

            if (row.WorkspaceStatus == WorkspaceStatus.Paused)
            {
                return new CampaignSendStartGateResult.Blocked
                {
                    Message = "Workspace is paused.",
                };
            }

            var account = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.RestaurantId == row.RestaurantId,
                    cancellationToken
                );
            if (account == null)
            {
                return new CampaignSendStartGateResult.Clear();
            }

            var deny = OperatorBillingLockEvaluator.EvaluateSendOrReserveDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(account),
                _utcNow()
            );
            if (deny == null)
            {
                return new CampaignSendStartGateResult.Clear();
            }

            if (deny == OperatorBillingLockEvaluator.SoftLock)
            {
                return new CampaignSendStartGateResult.SoftLocked();
            }

            return new CampaignSendStartGateResult.Blocked { Message = deny };
        }
    }
}
