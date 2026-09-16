namespace StudentRegistrar.Api.DTOs;

/// <summary>
/// Admin-facing view of tenant settings. Never includes the actual invite password value —
/// only whether it's configured, so the current secret is never round-tripped to the client.
/// </summary>
public sealed class TenantSettingsDto
{
    public bool InitialInvitePasswordConfigured { get; set; }
}

public sealed class UpdateTenantSettingsRequest
{
    /// <summary>
    /// New initial invite password for bulk/CSV member import. Must pass the realm's password
    /// policy (or the conservative fallback baseline if the policy can't be retrieved).
    /// </summary>
    public string InitialInvitePassword { get; set; } = string.Empty;
}
