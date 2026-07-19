using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/tenant-access-request")]
[AllowAnonymous]
public class TenantAccessRequestController : ControllerBase
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly StudentRegistrarDbContext _dbContext;

    public TenantAccessRequestController(
        ITenantContextAccessor tenantContextAccessor,
        StudentRegistrarDbContext dbContext)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<Dictionary<string, string>>> Get()
    {
        var tenant = _tenantContextAccessor.TenantContext?.Tenant
            ?? await ResolveSelfHostedTenantAsync();

        if (tenant is null || string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            return NotFound(new { error = "Tenant contact not found" });
        }

        return Ok(new Dictionary<string, string>
        {
            ["adminEmail"] = tenant.AdminEmail
        });
    }

    private async Task<Tenant?> ResolveSelfHostedTenantAsync()
    {
        if (_tenantContextAccessor.TenantContext?.DeploymentMode != DeploymentMode.SelfHosted)
        {
            return null;
        }

        var defaultTenant = await _dbContext.Tenants.FindAsync(TenantContext.DefaultTenantId);
        if (defaultTenant is not null && !string.IsNullOrWhiteSpace(defaultTenant.AdminEmail))
        {
            return defaultTenant;
        }

        return await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive && !string.IsNullOrWhiteSpace(t.AdminEmail))
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
