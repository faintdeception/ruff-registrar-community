using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
    Task<PaymentDto?> GetPaymentByIdAsync(Guid id);
    Task<IEnumerable<PaymentDto>> GetPaymentsByAccountHolderAsync(Guid accountHolderId);
    Task<IEnumerable<PaymentDto>> GetPaymentsByEnrollmentAsync(Guid enrollmentId);
    Task<IEnumerable<PaymentDto>> GetPaymentsByTypeAsync(PaymentType paymentType);
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createDto);
    Task<PaymentDto?> UpdatePaymentAsync(Guid id, UpdatePaymentDto updateDto);
    Task<bool> DeletePaymentAsync(Guid id);
    Task<decimal> GetTotalPaidByAccountHolderAsync(Guid accountHolderId, PaymentType? type = null);
    Task<decimal> GetTotalPaidByEnrollmentAsync(Guid enrollmentId);
    Task<IEnumerable<PaymentDto>> GetPaymentHistoryAsync(Guid accountHolderId, DateTime? fromDate = null, DateTime? toDate = null);
}
