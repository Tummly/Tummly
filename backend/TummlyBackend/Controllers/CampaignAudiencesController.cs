using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers;

[ApiController, Authorize, Route("api/campaign-audiences")]
public sealed class CampaignAudiencesController(ApplicationDbContext db,
    IRestaurantPermissionHelper permissions,
    ICampaignEligibilityService eligibility) : ControllerBase
{
    public sealed record CreateRequest(int LocationId, CampaignAudienceDefinition Definition, string? Name);

    private async Task<IActionResult?> Gate(int locationId)
    {
        var decision = await permissions.AuthorizeLocationAsync(User, OperatorAreaIds.Campaigns,
            HttpMethods.IsGet(Request.Method) ? PermissionLevel.View : PermissionLevel.Manage, locationId);
        var denied = decision.ToHttpResult();
        if (denied != null) return denied;
        return null;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRequest request, CancellationToken ct)
    {
        var denied = await Gate(request.LocationId);
        if (denied != null) return denied;
        try
        {
            await OperatorBillingLockGate.EnsurePaidWriteAllowedForLocationAsync(db, request.LocationId, ct);
            var key = await CampaignAudienceStore.CreateAsync(db, request.LocationId, request.Definition, request.Name, ct);
            return Ok(new { audienceKey = key, name = request.Name,
                definition = CampaignAudienceStore.Load(db, request.LocationId, key),
                preview = await eligibility.EvaluateAsync(request.LocationId, key, ct) });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> List(int locationId, CancellationToken ct)
    {
        var denied = await Gate(locationId);
        if (denied != null) return denied;
        var rows = await db.CampaignAudiences.AsNoTracking()
            .Where(a => a.RestaurantLocationId == locationId && a.Name != null)
            .OrderBy(a => a.Name).Select(a => new { a.Id, a.Name, a.CreatedAtUtc }).ToListAsync(ct);
        return Ok(rows.Select(a => new { audienceKey = $"audience:{a.Id:N}", a.Name, a.CreatedAtUtc }));
    }
}
