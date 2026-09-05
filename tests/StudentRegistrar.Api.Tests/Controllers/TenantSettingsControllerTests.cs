using Microsoft.AspNetCore.Mvc;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class TenantSettingsControllerTests
{
    private readonly Mock<ITenantSettingsService> _tenantSettingsService = new();
    private readonly TenantSettingsController _controller;

    public TenantSettingsControllerTests()
    {
        _controller = new TenantSettingsController(_tenantSettingsService.Object);
    }

    [Fact]
    public async Task Get_ReturnsOkWithSettings()
    {
        var dto = new TenantSettingsDto { InitialInvitePasswordConfigured = true };
        _tenantSettingsService.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await _controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task UpdateInitialInvitePassword_WhenValid_ReturnsOk()
    {
        var request = new UpdateTenantSettingsRequest { InitialInvitePassword = "Correct-Horse-99" };
        var dto = new TenantSettingsDto { InitialInvitePasswordConfigured = true };
        _tenantSettingsService
            .Setup(s => s.UpdateInitialInvitePasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.UpdateInitialInvitePassword(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task UpdateInitialInvitePassword_WhenInsecure_ReturnsBadRequestWithReasons()
    {
        var request = new UpdateTenantSettingsRequest { InitialInvitePassword = "password" };
        var reasons = new[] { "must be at least 12 characters" };
        _tenantSettingsService
            .Setup(s => s.UpdateInitialInvitePasswordAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InsecureInitialInvitePasswordException(reasons));

        var result = await _controller.UpdateInitialInvitePassword(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }
}
