using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/tenant-payment-connect")]
[Authorize(Roles = "Administrator")]
public class TenantPaymentConnectController : ControllerBase
{
    private readonly ITenantPaymentConnectService _tenantPaymentConnectService;

    public TenantPaymentConnectController(ITenantPaymentConnectService tenantPaymentConnectService)
    {
        _tenantPaymentConnectService = tenantPaymentConnectService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<TenantPaymentConnectStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await _tenantPaymentConnectService.GetStatusAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("status/refresh")]
    public async Task<ActionResult<TenantPaymentConnectStatusDto>> RefreshStatus(CancellationToken cancellationToken)
    {
        var result = await _tenantPaymentConnectService.RefreshStatusAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("onboarding-link")]
    public async Task<ActionResult<TenantPaymentConnectOnboardingLinkDto>> CreateOnboardingLink(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tenantPaymentConnectService.CreateOnboardingLinkAsync(cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
