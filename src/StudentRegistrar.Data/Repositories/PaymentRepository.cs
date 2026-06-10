using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Models;

namespace StudentRegistrar.Data.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly StudentRegistrarDbContext _context;

    public PaymentRepository(StudentRegistrarDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.AccountHolder)
            .Include(p => p.Enrollment)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return null;
        }

        var normalizedTransactionId = transactionId.Trim();
        return await _context.Payments
            .Include(p => p.AccountHolder)
            .Include(p => p.Enrollment)
            .FirstOrDefaultAsync(p => p.TransactionId == normalizedTransactionId);
    }

    public async Task<IEnumerable<Payment>> GetByAccountHolderIdAsync(Guid accountHolderId)
    {
        return await _context.Payments
            .Include(p => p.AccountHolder)
            .Include(p => p.Enrollment)
            .Where(p => p.AccountHolderId == accountHolderId)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByEnrollmentIdAsync(Guid enrollmentId)
    {
        return await _context.Payments
            .Include(p => p.AccountHolder)
            .Include(p => p.Enrollment)
            .Where(p => p.EnrollmentId == enrollmentId)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByTypeAsync(PaymentType paymentType)
    {
        return await _context.Payments
            .Include(p => p.AccountHolder)
            .Include(p => p.Enrollment)
            .Where(p => p.PaymentType == paymentType)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        return await _context.Payments
            .Include(p => p.AccountHolder)
            .Include(p => p.Enrollment)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        payment.CreatedAt = DateTime.UtcNow;
        
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(payment.Id) ?? payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(payment.Id) ?? payment;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
            return false;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<decimal> GetTotalPaidByAccountHolderAsync(Guid accountHolderId, PaymentType? type = null)
    {
        var query = _context.Payments
            .Where(p => p.AccountHolderId == accountHolderId);

        if (type.HasValue)
        {
            query = query.Where(p => p.PaymentType == type.Value);
        }

        return await query.SumAsync(p => p.Amount);
    }

    public async Task<decimal> GetTotalPaidByEnrollmentAsync(Guid enrollmentId)
    {
        return await _context.Payments
            .Where(p => p.EnrollmentId == enrollmentId)
            .SumAsync(p => p.Amount);
    }

    public async Task<IEnumerable<Payment>> GetPaymentHistoryAsync(Guid accountHolderId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Payments
            .Include(p => p.AccountHolder)
            .Include(p => p.Enrollment)
            .Where(p => p.AccountHolderId == accountHolderId);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= toDate.Value);
        }

        return await query
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Payments.AnyAsync(p => p.Id == id);
    }
}
