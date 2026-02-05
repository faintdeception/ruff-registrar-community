using StudentRegistrar.Data;

namespace StudentRegistrar.Api.Services.Infrastructure;

/// <summary>
/// Bridges the API layer's ITenantContext to the Data layer's ITenantProvider.
/// This allows the DbContext to access tenant information without a direct
/// dependency on the API layer.
/// </summary>
public class TenantContextProvider : ITenantProvider
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly bool _isSaaSMode;

    public TenantContextProvider(
        ITenantContextAccessor tenantContextAccessor, 
        IConfiguration configuration)
    {
        _tenantContextAccessor = tenantContextAccessor;
        
        // Cache deployment mode determination
        var modeString = configuration["DEPLOYMENT_MODE"] 
            ?? Environment.GetEnvironmentVariable("DEPLOYMENT_MODE") 
            ?? "selfhosted";
        _isSaaSMode = !modeString.Equals("selfhosted", StringComparison.OrdinalIgnoreCase);
    }

    public Guid? CurrentTenantId => _tenantContextAccessor.TenantContext?.TenantId;

    public bool ShouldApplyTenantFilter
    {
        get
        {
            // Self-hosted mode: no tenant filtering
            if (!_isSaaSMode)
            {
                return false;
            }

            // SaaS mode: apply filter only if we have a tenant context
            return _tenantContextAccessor.TenantContext != null;
        }
    }
}
