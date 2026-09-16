using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Models;

/// <summary>
/// Admin-configurable, tenant-scoped settings that don't belong on <see cref="Tenant"/> itself.
/// One row per tenant (1:1). New admin-settable knobs should be added here rather than on Tenant.
/// </summary>
public class TenantSettings : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Data-protection-encrypted initial password assigned to bulk/CSV-imported members.
    /// Always paired with a Keycloak "must change password on first login" requirement.
    /// Null/blank means the host has not configured this yet, and bulk import must be refused.
    /// </summary>
    [MaxLength(2000)]
    public string? InitialInvitePasswordEncrypted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
