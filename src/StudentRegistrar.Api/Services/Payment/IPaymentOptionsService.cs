using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface IPaymentOptionsService
{
    Task<PaymentOptionsDto> GetCurrentTenantPaymentOptionsAsync(CancellationToken cancellationToken = default);
    Task<PaymentOptionsDto> UpdateCurrentTenantPaymentOptionsAsync(UpdatePaymentOptionsDto updateDto, CancellationToken cancellationToken = default);
}