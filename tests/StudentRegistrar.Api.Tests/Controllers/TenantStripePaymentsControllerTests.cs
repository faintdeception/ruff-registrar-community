using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class TenantStripePaymentsControllerTests
{
    private readonly Mock<ITenantStripePaymentService> _paymentService = new();
    private readonly Mock<ILogger<TenantStripePaymentsController>> _logger = new();
    private readonly TenantStripePaymentsController _controller;

    public TenantStripePaymentsControllerTests()
    {
        _controller = new TenantStripePaymentsController(_paymentService.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateCheckoutSession_WhenSuccessful_ReturnsOk()
    {
        var request = new CreateTenantStripeCheckoutSessionDto
        {
            AccountHolderId = Guid.NewGuid(),
            Amount = 99.50m,
            PaymentType = PaymentType.CourseFee,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        var response = new TenantStripeCheckoutSessionDto
        {
            PaymentId = Guid.NewGuid(),
            SessionId = "cs_test_123",
            CheckoutUrl = "https://checkout.stripe.com/pay/cs_test_123"
        };

        _paymentService
            .Setup(s => s.CreateCheckoutSessionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CreateCheckoutSession(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task CreateCheckoutSession_WhenInvalidOperation_ReturnsBadRequest()
    {
        var request = new CreateTenantStripeCheckoutSessionDto
        {
            AccountHolderId = Guid.NewGuid(),
            Amount = 99.50m,
            PaymentType = PaymentType.CourseFee,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        _paymentService
            .Setup(s => s.CreateCheckoutSessionAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connect a Stripe account before collecting payments."));

        var result = await _controller.CreateCheckoutSession(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Connect a Stripe account before collecting payments.", badRequest.Value);
    }

    [Fact]
    public async Task HandleWebhook_WhenSignatureMissing_ReturnsBadRequest()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.HandleWebhook(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Stripe-Signature header is required.", badRequest.Value);
    }

    [Fact]
    public async Task HandleWebhook_WhenServiceReturnsFailure_ReturnsServerError()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Stripe-Signature"] = "t=123,v1=abc";
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
        _controller.ControllerContext = new ControllerContext { HttpContext = context };

        _paymentService
            .Setup(s => s.HandleWebhookAsync("{}", "t=123,v1=abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantStripeWebhookResultDto
            {
                Success = false,
                Processed = true,
                Message = "processing failed"
            });

        var result = await _controller.HandleWebhook(CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        Assert.Equal("processing failed", serverError.Value);
    }

    [Fact]
    public async Task HandleWebhook_WhenServiceSucceeds_ReturnsOk()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Stripe-Signature"] = "t=123,v1=abc";
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
        _controller.ControllerContext = new ControllerContext { HttpContext = context };

        _paymentService
            .Setup(s => s.HandleWebhookAsync("{}", "t=123,v1=abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantStripeWebhookResultDto
            {
                Success = true,
                Processed = true,
                Message = "ok"
            });

        var result = await _controller.HandleWebhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
    }
}
