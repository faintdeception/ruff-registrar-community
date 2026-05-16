using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services.Infrastructure;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/tenant/features")]
[Authorize]
public class TenantFeaturesController : ControllerBase
{
    private readonly ITenantEntitlementService _tenantEntitlementService;

    public TenantFeaturesController(ITenantEntitlementService tenantEntitlementService)
    {
        _tenantEntitlementService = tenantEntitlementService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantFeaturesDto>> Get(CancellationToken cancellationToken)
    {
        var snapshot = await _tenantEntitlementService.GetSnapshotAsync(cancellationToken);
        return Ok(new TenantFeaturesDto
        {
            SubscriptionTier = snapshot.SubscriptionTier.ToString(),
            IsSelfHostedMode = snapshot.IsSelfHostedMode,
            HasBranding = snapshot.HasBranding,
            HasPayments = snapshot.HasPayments,
            HasMembershipFees = snapshot.HasMembershipFees,
            HasPrioritySupport = snapshot.HasPrioritySupport,
            EnabledFeatures = snapshot.EnabledFeatures.ToList()
        });
    }
}