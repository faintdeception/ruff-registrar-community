using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class CourseInstructorsControllerTests
{
    private readonly Mock<ICourseInstructorService> _mockService = new();
    private readonly CourseInstructorsController _controller;

    public CourseInstructorsControllerTests()
    {
        _controller = new CourseInstructorsController(
            _mockService.Object,
            NullLogger<CourseInstructorsController>.Instance);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    /// <summary>
    /// Builds a JWT with realm_access containing the given role and sets it on the controller's
    /// Authorization header. The controller reads roles via JwtSecurityTokenHandler.ReadJwtToken,
    /// so no signature validation is required.
    /// </summary>
    private void SetAdminJwt(string role = "Administrator")
    {
        var realmAccess = $$$"""{"roles":["{{{role}}}"]}""";
        var token = new JwtSecurityToken(claims: [new Claim("realm_access", realmAccess)]);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        _controller.ControllerContext.HttpContext.Request.Headers["Authorization"] = $"Bearer {jwt}";
    }

    private static CourseInstructorDto MakeDto(Guid? id = null, Guid? courseId = null,
        string firstName = "Alice", string lastName = "Smith")
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            CourseId = courseId ?? Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = "alice@example.com"
        };

    // -------------------------------------------------------------------------
    // GET /api/courseinstructors
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCourseInstructors_ReturnsOkWithList()
    {
        var instructors = new List<CourseInstructorDto> { MakeDto(), MakeDto(firstName: "Bob", lastName: "Jones") };
        _mockService.Setup(s => s.GetAllCourseInstructorsAsync()).ReturnsAsync(instructors);

        var result = await _controller.GetCourseInstructors();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(instructors);
    }

    // -------------------------------------------------------------------------
    // GET /api/courseinstructors/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCourseInstructor_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id: id);
        _mockService.Setup(s => s.GetCourseInstructorByIdAsync(id)).ReturnsAsync(dto);

        var result = await _controller.GetCourseInstructor(id);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetCourseInstructor_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetCourseInstructorByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((CourseInstructorDto?)null);

        var result = await _controller.GetCourseInstructor(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -------------------------------------------------------------------------
    // GET /api/courseinstructors/course/{courseId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCourseInstructorsByCourse_ReturnsOkWithList()
    {
        var courseId = Guid.NewGuid();
        var instructors = new List<CourseInstructorDto> { MakeDto(courseId: courseId) };
        _mockService.Setup(s => s.GetCourseInstructorsByCourseIdAsync(courseId)).ReturnsAsync(instructors);

        var result = await _controller.GetCourseInstructorsByCourse(courseId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(instructors);
    }

    // -------------------------------------------------------------------------
    // POST /api/courseinstructors (Administrator only via manual JWT check)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateCourseInstructor_AsAdmin_ReturnsCreatedAtAction()
    {
        SetAdminJwt();
        var courseId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var createDto = new CreateCourseInstructorDto
        {
            CourseId = courseId,
            FirstName = "Carol",
            LastName = "White",
            Email = "carol@example.com",
            IsPrimary = true
        };
        var created = new CourseInstructorDto { Id = id, CourseId = courseId, FirstName = "Carol", LastName = "White" };
        _mockService.Setup(s => s.CreateCourseInstructorAsync(createDto)).ReturnsAsync(created);

        var result = await _controller.CreateCourseInstructor(createDto);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(CourseInstructorsController.GetCourseInstructor));
        createdResult.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task CreateCourseInstructor_NonAdmin_ReturnsForbid()
    {
        // No Authorization header → GetUserRole() returns "" → Forbid
        var result = await _controller.CreateCourseInstructor(new CreateCourseInstructorDto
        {
            CourseId = Guid.NewGuid(), FirstName = "X", LastName = "Y"
        });

        result.Result.Should().BeOfType<ForbidResult>();
        _mockService.Verify(s => s.CreateCourseInstructorAsync(It.IsAny<CreateCourseInstructorDto>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // PUT /api/courseinstructors/{id} (Administrator only via manual JWT check)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateCourseInstructor_AsAdmin_Found_ReturnsOk()
    {
        SetAdminJwt();
        var id = Guid.NewGuid();
        var updateDto = new UpdateCourseInstructorDto { FirstName = "Alice", LastName = "Updated" };
        var updated = MakeDto(id: id, lastName: "Updated");
        _mockService.Setup(s => s.UpdateCourseInstructorAsync(id, updateDto)).ReturnsAsync(updated);

        var result = await _controller.UpdateCourseInstructor(id, updateDto);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(updated);
    }

    [Fact]
    public async Task UpdateCourseInstructor_AsAdmin_NotFound_ReturnsNotFound()
    {
        SetAdminJwt();
        _mockService.Setup(s => s.UpdateCourseInstructorAsync(It.IsAny<Guid>(), It.IsAny<UpdateCourseInstructorDto>()))
            .ReturnsAsync((CourseInstructorDto?)null);

        var result = await _controller.UpdateCourseInstructor(Guid.NewGuid(), new UpdateCourseInstructorDto
        {
            FirstName = "X", LastName = "Y"
        });

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateCourseInstructor_NonAdmin_ReturnsForbid()
    {
        var result = await _controller.UpdateCourseInstructor(Guid.NewGuid(), new UpdateCourseInstructorDto
        {
            FirstName = "X", LastName = "Y"
        });

        result.Result.Should().BeOfType<ForbidResult>();
        _mockService.Verify(s => s.UpdateCourseInstructorAsync(It.IsAny<Guid>(), It.IsAny<UpdateCourseInstructorDto>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/courseinstructors/{id} (Administrator only via manual JWT check)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteCourseInstructor_AsAdmin_Exists_ReturnsNoContent()
    {
        SetAdminJwt();
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteCourseInstructorAsync(id)).ReturnsAsync(true);

        var result = await _controller.DeleteCourseInstructor(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteCourseInstructor_AsAdmin_NotFound_ReturnsNotFound()
    {
        SetAdminJwt();
        _mockService.Setup(s => s.DeleteCourseInstructorAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.DeleteCourseInstructor(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteCourseInstructor_NonAdmin_ReturnsForbid()
    {
        var result = await _controller.DeleteCourseInstructor(Guid.NewGuid());

        result.Should().BeOfType<ForbidResult>();
        _mockService.Verify(s => s.DeleteCourseInstructorAsync(It.IsAny<Guid>()), Times.Never);
    }
}
