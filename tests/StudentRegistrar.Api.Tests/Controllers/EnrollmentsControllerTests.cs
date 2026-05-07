using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class EnrollmentsControllerTests
{
    private readonly Mock<IEnrollmentService> _mockService = new();
    private readonly Mock<ILogger<EnrollmentsController>> _mockLogger = new();
    private readonly EnrollmentsController _controller;

    public EnrollmentsControllerTests()
    {
        _controller = new EnrollmentsController(_mockService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // -------------------------------------------------------------------------
    // GET /api/enrollments (requires admin)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetEnrollments_NoAdminToken_ReturnsForbid()
    {
        // No Authorization header → GetUserRole returns "" → not Administrator
        var result = await _controller.GetEnrollments();

        result.Should().BeOfType<ForbidResult>();
    }

    // -------------------------------------------------------------------------
    // GET /api/enrollments/my
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMyEnrollments_NoUser_ReturnsUnauthorized()
    {
        // No claims → GetKeycloakUserId returns "" → Unauthorized
        var result = await _controller.GetMyEnrollments();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetMyEnrollments_AccountHolderNotFound_ReturnsNotFound()
    {
        SetKeycloakSubClaim("kc-user-1");

        _mockService
            .Setup(s => s.GetMyEnrollmentsAsync("kc-user-1"))
            .ThrowsAsync(new InvalidOperationException("Account holder not found."));

        var result = await _controller.GetMyEnrollments();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMyEnrollments_ServiceError_Returns500()
    {
        SetKeycloakSubClaim("kc-user-1");

        _mockService
            .Setup(s => s.GetMyEnrollmentsAsync("kc-user-1"))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _controller.GetMyEnrollments();

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetMyEnrollments_ValidUser_ReturnsOk()
    {
        SetKeycloakSubClaim("kc-user-1");

        var enrollments = new List<EnrollmentDetailDto>
        {
            new() { Id = Guid.NewGuid().ToString(), EnrollmentType = "Enrolled", CourseName = "Algebra I" }
        };

        _mockService
            .Setup(s => s.GetMyEnrollmentsAsync("kc-user-1"))
            .ReturnsAsync(enrollments);

        var result = await _controller.GetMyEnrollments();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(enrollments);
    }

    // -------------------------------------------------------------------------
    // POST /api/enrollments/{id}/withdraw
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Withdraw_EnrollmentNotFound_ReturnsNotFound()
    {
        SetKeycloakSubClaim("kc-user-1");
        var id = Guid.NewGuid();

        _mockService
            .Setup(s => s.WithdrawAsync(id, "kc-user-1", null))
            .ThrowsAsync(new KeyNotFoundException("Enrollment not found."));

        var result = await _controller.Withdraw(id, null);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Withdraw_InvalidState_ReturnsBadRequest()
    {
        SetKeycloakSubClaim("kc-user-1");
        var id = Guid.NewGuid();

        _mockService
            .Setup(s => s.WithdrawAsync(id, "kc-user-1", null))
            .ThrowsAsync(new InvalidOperationException("Cannot withdraw."));

        var result = await _controller.Withdraw(id, null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Withdraw_UnauthorizedStudent_ReturnsForbid()
    {
        SetKeycloakSubClaim("kc-user-1");
        var id = Guid.NewGuid();

        _mockService
            .Setup(s => s.WithdrawAsync(id, "kc-user-1", null))
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.Withdraw(id, null);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Withdraw_Success_ReturnsOk()
    {
        SetKeycloakSubClaim("kc-user-1");
        var id = Guid.NewGuid();
        var dto = new EnrollmentDetailDto { Id = id.ToString(), EnrollmentType = "Withdrawn" };

        _mockService
            .Setup(s => s.WithdrawAsync(id, "kc-user-1", null))
            .ReturnsAsync(dto);

        var result = await _controller.Withdraw(id, null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(dto);
    }

    // -------------------------------------------------------------------------
    // POST /api/enrollments/{id}/cancel (admin only)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Cancel_NoAdminToken_ReturnsForbid()
    {
        var result = await _controller.Cancel(Guid.NewGuid());

        result.Should().BeOfType<ForbidResult>();
    }

    // -------------------------------------------------------------------------
    // POST /api/enrollments/{id}/promote (admin only)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Promote_NoAdminToken_ReturnsForbid()
    {
        var result = await _controller.Promote(Guid.NewGuid());

        result.Should().BeOfType<ForbidResult>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetKeycloakSubClaim(string userId)
    {
        var claims = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim("sub", userId) },
                authenticationType: "Test"));

        _controller.ControllerContext.HttpContext.User = claims;
    }
}
