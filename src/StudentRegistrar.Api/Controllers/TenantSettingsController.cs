using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/tenant-settings")]
[Authorize(Roles = "Administrator")]
public class TenantSettingsController : ControllerBase
{
    private readonly ITenantSettingsService _tenantSettingsService;

    public TenantSettingsController(ITenantSettingsService tenantSettingsService)
    {
        _tenantSettingsService = tenantSettingsService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var result = await _tenantSettingsService.GetSettingsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("initial-invite-password")]
    public async Task<ActionResult<TenantSettingsDto>> UpdateInitialInvitePassword(
        [FromBody] UpdateTenantSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tenantSettingsService.UpdateInitialInvitePasswordAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InsecureInitialInvitePasswordException ex)
        {
            return BadRequest(new { message = ex.Message, reasons = ex.FailureReasons });
        }
    }
}
