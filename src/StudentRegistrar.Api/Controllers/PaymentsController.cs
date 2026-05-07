using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    // Modern Guid-based endpoints
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments()
    {
        var payments = await _paymentService.GetAllPaymentsAsync();
        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(Guid id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    [HttpGet("account-holder/{accountHolderId:guid}")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByAccountHolder(Guid accountHolderId)
    {
        var payments = await _paymentService.GetPaymentsByAccountHolderAsync(accountHolderId);
        return Ok(payments);
    }

    [HttpGet("enrollment/{enrollmentId:guid}")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByEnrollment(Guid enrollmentId)
    {
        var payments = await _paymentService.GetPaymentsByEnrollmentAsync(enrollmentId);
        return Ok(payments);
    }

    [HttpGet("type/{paymentType}")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByType(PaymentType paymentType)
    {
        var payments = await _paymentService.GetPaymentsByTypeAsync(paymentType);
        return Ok(payments);
    }

    [HttpGet("account-holder/{accountHolderId:guid}/history")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentHistory(
        Guid accountHolderId, 
        [FromQuery] DateTime? fromDate = null, 
        [FromQuery] DateTime? toDate = null)
    {
        var payments = await _paymentService.GetPaymentHistoryAsync(accountHolderId, fromDate, toDate);
        return Ok(payments);
    }

    [HttpGet("account-holder/{accountHolderId:guid}/total")]
    public async Task<ActionResult<decimal>> GetTotalPaidByAccountHolder(
        Guid accountHolderId, 
        [FromQuery] PaymentType? type = null)
    {
        var total = await _paymentService.GetTotalPaidByAccountHolderAsync(accountHolderId, type);
        return Ok(total);
    }

    [HttpGet("enrollment/{enrollmentId:guid}/total")]
    public async Task<ActionResult<decimal>> GetTotalPaidByEnrollment(Guid enrollmentId)
    {
        var total = await _paymentService.GetTotalPaidByEnrollmentAsync(enrollmentId);
        return Ok(total);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<PaymentDto>> CreatePayment(CreatePaymentDto createPaymentDto)
    {
        try
        {
            var payment = await _paymentService.CreatePaymentAsync(createPaymentDto);
            return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return BadRequest("Error creating payment");
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<PaymentDto>> UpdatePayment(Guid id, UpdatePaymentDto updatePaymentDto)
    {
        var payment = await _paymentService.UpdatePaymentAsync(id, updatePaymentDto);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeletePayment(Guid id)
    {
        var result = await _paymentService.DeletePaymentAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
