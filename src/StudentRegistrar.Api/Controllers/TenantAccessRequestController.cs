using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.Services.Infrastructure;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/tenant-access-request")]
[AllowAnonymous]
public class TenantAccessRequestController : ControllerBase
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantAccessRequestController(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    [HttpGet]
    public ActionResult<Dictionary<string, string>> Get()
    {
        var tenant = _tenantContextAccessor.TenantContext?.Tenant;
        if (tenant is null || string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            return NotFound(new { error = "Tenant contact not found" });
        }

        return Ok(new Dictionary<string, string>
        {
            ["adminEmail"] = tenant.AdminEmail
        });
    }
}
