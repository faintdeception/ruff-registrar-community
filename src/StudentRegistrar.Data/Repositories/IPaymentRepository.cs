using StudentRegistrar.Models;

namespace StudentRegistrar.Data.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByTransactionIdAsync(string transactionId);
    Task<IEnumerable<Payment>> GetByAccountHolderIdAsync(Guid accountHolderId);
    Task<IEnumerable<Payment>> GetByEnrollmentIdAsync(Guid enrollmentId);
    Task<IEnumerable<Payment>> GetByTypeAsync(PaymentType paymentType);
    Task<IEnumerable<Payment>> GetAllAsync();
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
    Task<bool> DeleteAsync(Guid id);
    Task<decimal> GetTotalPaidByAccountHolderAsync(Guid accountHolderId, PaymentType? type = null);
    Task<decimal> GetTotalPaidByEnrollmentAsync(Guid enrollmentId);
    Task<IEnumerable<Payment>> GetPaymentHistoryAsync(Guid accountHolderId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<bool> ExistsAsync(Guid id);
}
