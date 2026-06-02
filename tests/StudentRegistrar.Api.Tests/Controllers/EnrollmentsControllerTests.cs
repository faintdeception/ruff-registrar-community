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

        Assert.IsType<ForbidResult>(result);
    }

    // -------------------------------------------------------------------------
    // GET /api/enrollments/my
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMyEnrollments_NoUser_ReturnsUnauthorized()
    {
        // No claims → GetKeycloakUserId returns "" → Unauthorized
        var result = await _controller.GetMyEnrollments();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetMyEnrollments_AccountHolderNotFound_ReturnsNotFound()
    {
        SetKeycloakSubClaim("kc-user-1");

        _mockService
            .Setup(s => s.GetMyEnrollmentsAsync("kc-user-1"))
            .ThrowsAsync(new InvalidOperationException("Account holder not found."));

        var result = await _controller.GetMyEnrollments();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMyEnrollments_ServiceError_Returns500()
    {
        SetKeycloakSubClaim("kc-user-1");

        _mockService
            .Setup(s => s.GetMyEnrollmentsAsync("kc-user-1"))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _controller.GetMyEnrollments();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
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

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(enrollments, okResult.Value);
    }

    [Fact]
    public async Task GetMyTeachingRoster_NoEducatorToken_ReturnsForbid()
    {
        var result = await _controller.GetMyTeachingRoster();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMyTeachingRoster_Educator_ReturnsOk()
    {
        SetKeycloakSubClaim("kc-educator-1");
        SetAuthorizationRole("Educator");

        var roster = new List<EnrollmentDetailDto>
        {
            new() { Id = Guid.NewGuid().ToString(), CourseName = "Biology Lab", StudentName = "Casey Morgan" }
        };

        _mockService
            .Setup(s => s.GetMyTeachingRosterAsync("kc-educator-1", null))
            .ReturnsAsync(roster);

        var result = await _controller.GetMyTeachingRoster();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(roster, okResult.Value);
    }

    [Fact]
    public async Task GetMyTeachingRoster_UnassignedCourse_ReturnsForbid()
    {
        var courseId = Guid.NewGuid();
        SetKeycloakSubClaim("kc-educator-1");
        SetAuthorizationRole("Educator");

        _mockService
            .Setup(s => s.GetMyTeachingRosterAsync("kc-educator-1", courseId))
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.GetMyTeachingRoster(courseId);

        Assert.IsType<ForbidResult>(result);
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

        Assert.IsType<NotFoundObjectResult>(result);
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

        Assert.IsType<BadRequestObjectResult>(result);
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

        Assert.IsType<ForbidResult>(result);
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

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, okResult.Value);
    }

    // -------------------------------------------------------------------------
    // POST /api/enrollments/{id}/cancel (admin only)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Cancel_NoAdminToken_ReturnsForbid()
    {
        var result = await _controller.Cancel(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
    }

    // -------------------------------------------------------------------------
    // POST /api/enrollments/{id}/promote (admin only)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Promote_NoAdminToken_ReturnsForbid()
    {
        var result = await _controller.Promote(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
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

    private void SetAuthorizationRole(string role)
    {
        var payload = role switch
        {
            "Administrator" => "eyJyZWFsbV9hY2Nlc3MiOnsicm9sZXMiOlsiQWRtaW5pc3RyYXRvciJdfX0",
            "Educator" => "eyJyZWFsbV9hY2Nlc3MiOnsicm9sZXMiOlsiRWR1Y2F0b3IiXX19",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(payload))
        {
            _controller.ControllerContext.HttpContext.Request.Headers.Authorization = string.Empty;
            return;
        }

        _controller.ControllerContext.HttpContext.Request.Headers.Authorization = $"Bearer eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.{payload}.";
    }
}
