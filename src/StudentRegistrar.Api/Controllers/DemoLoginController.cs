using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/demo-login")]
[AllowAnonymous]
public class DemoLoginController : ControllerBase
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IOptions<DemoLoginOptions> _options;
    private readonly StudentRegistrarDbContext _dbContext;

    public DemoLoginController(
        ITenantContextAccessor tenantContextAccessor,
        IOptions<DemoLoginOptions> options,
        StudentRegistrarDbContext dbContext)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _options = options;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<DemoLoginResponse>> Get()
    {
        var options = _options.Value;
        var context = _tenantContextAccessor.TenantContext;
        var subdomain = context?.Tenant?.Subdomain;

        // Self-hosted mode has no per-request tenant, so use the default tenant like the access-request endpoint does.
        if (subdomain is null && context?.DeploymentMode == DeploymentMode.SelfHosted)
        {
            subdomain = (await _dbContext.Tenants.FindAsync(TenantContext.DefaultTenantId))?.Subdomain;
        }

        var isDemoTenant = options.Enabled
            && !string.IsNullOrWhiteSpace(options.TenantSubdomain)
            && string.Equals(subdomain, options.TenantSubdomain, StringComparison.OrdinalIgnoreCase);

        if (!isDemoTenant)
        {
            return NotFound();
        }

        var accounts = new List<DemoLoginAccount>();
        if (options.Admin.IsComplete)
        {
            accounts.Add(new DemoLoginAccount("admin", options.Admin.Username!, options.Admin.Password!));
        }

        if (options.Member.IsComplete)
        {
            accounts.Add(new DemoLoginAccount("member", options.Member.Username!, options.Member.Password!));
        }

        return accounts.Count == 0 ? NotFound() : Ok(new DemoLoginResponse(accounts));
    }
}

public record DemoLoginResponse(IReadOnlyList<DemoLoginAccount> Accounts);

public record DemoLoginAccount(string Role, string Username, string Password);
