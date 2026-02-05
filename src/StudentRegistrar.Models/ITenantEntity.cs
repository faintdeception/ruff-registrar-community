namespace StudentRegistrar.Models;

/// <summary>
/// Interface for entities that belong to a tenant.
/// All tenant-aware entities must implement this interface.
/// In self-hosted mode, TenantId may be a fixed default value.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The tenant (organization) this entity belongs to.
    /// Required for all tenant-scoped data in SaaS mode.
    /// </summary>
    Guid TenantId { get; set; }
}
