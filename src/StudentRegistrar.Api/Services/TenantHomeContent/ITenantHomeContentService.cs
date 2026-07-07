using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface ITenantHomeContentService
{
    Task<TenantHomeContentDto> GetHomeContentAsync(CancellationToken cancellationToken = default);
    Task<TenantHomeContentDto> UpdateHomeContentAsync(UpdateTenantHomeContentRequest request, CancellationToken cancellationToken = default);
}
