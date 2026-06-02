using Microsoft.AspNetCore.Mvc;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class TenantBillingControllerTests
{
    private readonly Mock<ITenantBillingService> _billingService = new();
    private readonly TenantBillingController _controller;

    public TenantBillingControllerTests()
    {
        _controller = new TenantBillingController(_billingService.Object);
    }

    [Fact]
    public async Task GetCurrentBilling_ReturnsOkWithStatus()
    {
        var dto = new TenantBillingStatusDto
        {
            IsSaaSMode = true,
            CanManageBilling = true,
            SubscriptionTier = "Pro",
            SubscriptionStatus = "Active"
        };
        _billingService.Setup(s => s.GetCurrentBillingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.GetCurrentBilling(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task ScheduleCancellationAtPeriodEnd_WhenSuccessful_ReturnsOk()
    {
        var dto = new TenantBillingCancellationDto
        {
            Subdomain = "sunrise",
            AccessEndsAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc),
            CancellationScheduled = true,
            Message = "Cancellation scheduled."
        };
        _billingService.Setup(s => s.ScheduleCancellationAtPeriodEndAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.ScheduleCancellationAtPeriodEnd(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task ScheduleCancellationAtPeriodEnd_WhenServiceRejectsRequest_ReturnsBadRequest()
    {
        _billingService
            .Setup(s => s.ScheduleCancellationAtPeriodEndAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cancellation is only available for active paid organizations."));

        var result = await _controller.ScheduleCancellationAtPeriodEnd(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Cancellation is only available for active paid organizations.", badRequest.Value);
    }

    [Fact]
    public async Task UndoScheduledCancellation_WhenSuccessful_ReturnsOk()
    {
        var dto = new TenantBillingCancellationDto
        {
            Subdomain = "sunrise",
            CancellationScheduled = false,
            Message = "Scheduled cancellation removed. Billing remains active."
        };
        _billingService.Setup(s => s.UndoScheduledCancellationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.UndoScheduledCancellation(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task UndoScheduledCancellation_WhenServiceRejectsRequest_ReturnsBadRequest()
    {
        _billingService
            .Setup(s => s.UndoScheduledCancellationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No scheduled cancellation is available to undo."));

        var result = await _controller.UndoScheduledCancellation(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No scheduled cancellation is available to undo.", badRequest.Value);
    }
}