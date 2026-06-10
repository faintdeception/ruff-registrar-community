using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/tenant-stripe-payments")]
[Authorize]
public class TenantStripePaymentsController : ControllerBase
{
    private readonly ITenantStripePaymentService _tenantStripePaymentService;
    private readonly ILogger<TenantStripePaymentsController> _logger;

    public TenantStripePaymentsController(
        ITenantStripePaymentService tenantStripePaymentService,
        ILogger<TenantStripePaymentsController> logger)
    {
        _tenantStripePaymentService = tenantStripePaymentService;
        _logger = logger;
    }

    [HttpPost("checkout-session")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<TenantStripeCheckoutSessionDto>> CreateCheckoutSession(
        CreateTenantStripeCheckoutSessionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tenantStripePaymentService.CreateCheckoutSessionAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature))
        {
            return BadRequest("Stripe-Signature header is required.");
        }

        string payload;
        using (var reader = new StreamReader(Request.Body))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        try
        {
            var result = await _tenantStripePaymentService.HandleWebhookAsync(payload, signature, cancellationToken);
            if (!result.Success)
            {
                _logger.LogError(
                    "Stripe tenant payment webhook failed. EventId={EventId} EventType={EventType} Message={Message}",
                    result.EventId,
                    result.EventType,
                    result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, result.Message);
            }

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
