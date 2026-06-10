using Microsoft.AspNetCore.Mvc;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class TenantPaymentConnectControllerTests
{
    private readonly Mock<ITenantPaymentConnectService> _connectService = new();
    private readonly TenantPaymentConnectController _controller;

    public TenantPaymentConnectControllerTests()
    {
        _controller = new TenantPaymentConnectController(_connectService.Object);
    }

    [Fact]
    public async Task GetStatus_ReturnsOkWithStatusDto()
    {
        var dto = new TenantPaymentConnectStatusDto
        {
            IsSaaSMode = true,
            HasPaymentFeatures = true,
            PlatformStripeConfigured = true,
            IsAvailable = true,
            IsConnected = false
        };
        _connectService.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.GetStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task GetStatus_WhenUnavailable_ReturnsOkWithUnavailableDto()
    {
        var dto = new TenantPaymentConnectStatusDto
        {
            IsSaaSMode = false,
            IsAvailable = false,
            UnavailableReason = "Tenant-owned Stripe Connect is available in SaaS deployments only."
        };
        _connectService.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.GetStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<TenantPaymentConnectStatusDto>(ok.Value);
        Assert.False(returned.IsAvailable);
        Assert.NotNull(returned.UnavailableReason);
    }

    [Fact]
    public async Task RefreshStatus_ReturnsOkWithRefreshedDto()
    {
        var dto = new TenantPaymentConnectStatusDto
        {
            IsSaaSMode = true,
            IsAvailable = true,
            IsConnected = true,
            StripeConnectAccountId = "acct_1234567890",
            ChargesEnabled = true,
            PayoutsEnabled = true,
            DetailsSubmitted = true
        };
        _connectService.Setup(s => s.RefreshStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.RefreshStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task CreateOnboardingLink_WhenSuccessful_ReturnsOkWithLink()
    {
        var dto = new TenantPaymentConnectOnboardingLinkDto
        {
            Url = "https://connect.stripe.com/setup/e/acct_1234567890/abc123",
            ExpiresAtUtc = new DateTime(2026, 6, 10, 1, 0, 0, DateTimeKind.Utc)
        };
        _connectService.Setup(s => s.CreateOnboardingLinkAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.CreateOnboardingLink(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task CreateOnboardingLink_WhenServiceRejectsRequest_ReturnsBadRequest()
    {
        _connectService
            .Setup(s => s.CreateOnboardingLinkAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Stripe Connect is not available for this tenant."));

        var result = await _controller.CreateOnboardingLink(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Stripe Connect is not available for this tenant.", badRequest.Value);
    }

    [Fact]
    public async Task CreateOnboardingLink_WhenSelfHosted_ReturnsBadRequest()
    {
        _connectService
            .Setup(s => s.CreateOnboardingLinkAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tenant-owned Stripe Connect is available in SaaS deployments only."));

        var result = await _controller.CreateOnboardingLink(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Tenant-owned Stripe Connect is available in SaaS deployments only.", badRequest.Value);
    }
}
