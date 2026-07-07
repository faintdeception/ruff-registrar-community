using Microsoft.AspNetCore.Mvc;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class TenantHomeContentControllerTests
{
    private readonly Mock<ITenantHomeContentService> _homeContentService = new();
    private readonly TenantHomeContentController _controller;

    public TenantHomeContentControllerTests()
    {
        _controller = new TenantHomeContentController(_homeContentService.Object);
    }

    [Fact]
    public async Task Get_ReturnsOkWithContent()
    {
        var dto = new TenantHomeContentDto
        {
            WelcomeTitle = "Welcome to Sunrise",
            WelcomeBlurb = "Custom blurb",
            HasCustomWelcomeTitle = true,
            HasCustomWelcomeBlurb = true
        };

        _homeContentService
            .Setup(s => s.GetHomeContentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsOk()
    {
        var request = new UpdateTenantHomeContentRequest
        {
            WelcomeTitle = "Welcome to Sunrise",
            WelcomeBlurb = "Custom blurb"
        };

        var dto = new TenantHomeContentDto
        {
            WelcomeTitle = "Welcome to Sunrise",
            WelcomeBlurb = "Custom blurb",
            HasCustomWelcomeTitle = true,
            HasCustomWelcomeBlurb = true
        };

        _homeContentService
            .Setup(s => s.UpdateHomeContentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Update(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task Update_WhenValidationFails_ReturnsBadRequest()
    {
        var request = new UpdateTenantHomeContentRequest
        {
            WelcomeTitle = new string('x', 121)
        };

        _homeContentService
            .Setup(s => s.UpdateHomeContentAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("WelcomeTitle must be 120 characters or fewer."));

        var result = await _controller.Update(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("WelcomeTitle must be 120 characters or fewer.", badRequest.Value);
    }
}
