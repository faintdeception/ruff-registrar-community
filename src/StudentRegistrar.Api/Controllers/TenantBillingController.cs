using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/tenant-billing")]
[Authorize(Roles = "Administrator")]
public class TenantBillingController : ControllerBase
{
    private readonly ITenantBillingService _tenantBillingService;

    public TenantBillingController(ITenantBillingService tenantBillingService)
    {
        _tenantBillingService = tenantBillingService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantBillingStatusDto>> GetCurrentBilling(CancellationToken cancellationToken)
    {
        var result = await _tenantBillingService.GetCurrentBillingAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("cancel-at-period-end")]
    public async Task<ActionResult<TenantBillingCancellationDto>> ScheduleCancellationAtPeriodEnd(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tenantBillingService.ScheduleCancellationAtPeriodEndAsync(cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("undo-cancel-at-period-end")]
    public async Task<ActionResult<TenantBillingCancellationDto>> UndoScheduledCancellation(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tenantBillingService.UndoScheduledCancellationAsync(cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}