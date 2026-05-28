using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface ITenantBillingService
{
    Task<TenantBillingStatusDto> GetCurrentBillingAsync(CancellationToken cancellationToken = default);
    Task<TenantBillingCancellationDto> ScheduleCancellationAtPeriodEndAsync(CancellationToken cancellationToken = default);
    Task<TenantBillingCancellationDto> UndoScheduledCancellationAsync(CancellationToken cancellationToken = default);
}