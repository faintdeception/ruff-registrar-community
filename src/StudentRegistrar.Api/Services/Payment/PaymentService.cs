using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;

    public PaymentService(IPaymentRepository paymentRepository, IMapper mapper)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
    {
        var payments = await _paymentRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByAccountHolderAsync(Guid accountHolderId)
    {
        var payments = await _paymentRepository.GetByAccountHolderIdAsync(accountHolderId);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByEnrollmentAsync(Guid enrollmentId)
    {
        var payments = await _paymentRepository.GetByEnrollmentIdAsync(enrollmentId);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByTypeAsync(PaymentType paymentType)
    {
        var payments = await _paymentRepository.GetByTypeAsync(paymentType);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createDto)
    {
        var payment = _mapper.Map<Payment>(createDto);
        var createdPayment = await _paymentRepository.CreateAsync(payment);
        return _mapper.Map<PaymentDto>(createdPayment);
    }

    public async Task<PaymentDto?> UpdatePaymentAsync(Guid id, UpdatePaymentDto updateDto)
    {
        var existingPayment = await _paymentRepository.GetByIdAsync(id);
        if (existingPayment == null)
            return null;

        _mapper.Map(updateDto, existingPayment);
        var updatedPayment = await _paymentRepository.UpdateAsync(existingPayment);
        return _mapper.Map<PaymentDto>(updatedPayment);
    }

    public async Task<bool> DeletePaymentAsync(Guid id)
    {
        return await _paymentRepository.DeleteAsync(id);
    }

    public async Task<decimal> GetTotalPaidByAccountHolderAsync(Guid accountHolderId, PaymentType? type = null)
    {
        return await _paymentRepository.GetTotalPaidByAccountHolderAsync(accountHolderId, type);
    }

    public async Task<decimal> GetTotalPaidByEnrollmentAsync(Guid enrollmentId)
    {
        return await _paymentRepository.GetTotalPaidByEnrollmentAsync(enrollmentId);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentHistoryAsync(Guid accountHolderId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var payments = await _paymentRepository.GetPaymentHistoryAsync(accountHolderId, fromDate, toDate);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }
}
