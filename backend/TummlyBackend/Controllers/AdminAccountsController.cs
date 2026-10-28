using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin/accounts")]
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : ControllerBase
    {
        private readonly IAdminAccountDirectoryService _directory;

        public AdminAccountsController(IAdminAccountDirectoryService directory)
        {
            _directory = directory;
        }

        [HttpGet]
        public async Task<IActionResult> ListAccounts(
            [FromQuery] string? search,
            [FromQuery] string? plan,
            [FromQuery] string? billingStatus,
            [FromQuery] DateTime? createdFrom,
            [FromQuery] DateTime? createdTo,
            [FromQuery] int skip = 0,
            [FromQuery] int take = AdminAccountDirectoryService.DefaultTake,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var result = await _directory.ListAsync(
                    new AdminAccountListQuery(
                        Search: search,
                        SubscriptionPlan: plan,
                        BillingStatus: billingStatus,
                        CreatedFrom: createdFrom == null
                            ? null
                            : DateTime.SpecifyKind(createdFrom.Value.Date, DateTimeKind.Utc),
                        // createdTo is an inclusive calendar day; the service bound is exclusive.
                        CreatedBefore: createdTo == null
                            ? null
                            : DateTime.SpecifyKind(createdTo.Value.Date.AddDays(1), DateTimeKind.Utc),
                        Skip: skip,
                        Take: take
                    ),
                    cancellationToken
                );

                return Ok(new
                {
                    success = true,
                    items = result.Items,
                    totalCount = result.TotalCount,
                    skip = result.Skip,
                    take = result.Take,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }
    }
}
