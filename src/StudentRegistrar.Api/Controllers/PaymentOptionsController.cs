using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/settings/payment-options")]
[Authorize(Roles = "Administrator")]
public class PaymentOptionsController : ControllerBase
{
    private readonly IPaymentOptionsService _paymentOptionsService;
    private readonly ILogger<PaymentOptionsController> _logger;

    public PaymentOptionsController(
        IPaymentOptionsService paymentOptionsService,
        ILogger<PaymentOptionsController> logger)
    {
        _paymentOptionsService = paymentOptionsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaymentOptionsDto>> GetPaymentOptions(CancellationToken cancellationToken)
    {
        var paymentOptions = await _paymentOptionsService.GetCurrentTenantPaymentOptionsAsync(cancellationToken);
        return Ok(paymentOptions);
    }

    [HttpPut]
    public async Task<ActionResult<PaymentOptionsDto>> UpdatePaymentOptions(
        UpdatePaymentOptionsDto updateDto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var paymentOptions = await _paymentOptionsService.UpdateCurrentTenantPaymentOptionsAsync(updateDto, cancellationToken);
            return Ok(paymentOptions);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Rejected payment-options update because the tenant does not have payment features.");
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rejected invalid payment-options update request.");
            return BadRequest(ex.Message);
        }
    }
}