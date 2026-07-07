namespace StudentRegistrar.Api.DTOs;

public sealed class TenantHomeContentDto
{
    public string WelcomeTitle { get; init; } = string.Empty;
    public string WelcomeBlurb { get; init; } = string.Empty;
    public bool HasCustomWelcomeTitle { get; init; }
    public bool HasCustomWelcomeBlurb { get; init; }
}

public sealed class UpdateTenantHomeContentRequest
{
    public string? WelcomeTitle { get; init; }
    public string? WelcomeBlurb { get; init; }
}
