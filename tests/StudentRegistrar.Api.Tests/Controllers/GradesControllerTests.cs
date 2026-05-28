using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class GradesControllerTests
{
    private readonly Mock<IGradeService> _mockService = new();
    private readonly GradesController _controller;

    public GradesControllerTests()
    {
        _controller = new GradesController(_mockService.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // -------------------------------------------------------------------------
    // GET /api/grades
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGrades_ReturnsOkWithList()
    {
        var grades = new List<GradeRecordDto>
        {
            new() { Id = Guid.NewGuid(), LetterGrade = "A", NumericGrade = 95m }
        };
        _mockService.Setup(s => s.GetAllGradesAsync()).ReturnsAsync(grades);

        var result = await _controller.GetGrades();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(grades, okResult.Value);
    }

    // -------------------------------------------------------------------------
    // GET /api/grades/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGrade_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new GradeRecordDto { Id = id, LetterGrade = "B+" };
        _mockService.Setup(s => s.GetGradeByIdAsync(id)).ReturnsAsync(dto);

        var result = await _controller.GetGrade(id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, okResult.Value);
    }

    [Fact]
    public async Task GetGrade_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetGradeByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GradeRecordDto?)null);

        var result = await _controller.GetGrade(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -------------------------------------------------------------------------
    // POST /api/grades
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateGrade_ReturnsCreatedAtAction()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var createDto = new CreateGradeRecordDto
        {
            StudentId = studentId,
            CourseId = courseId,
            LetterGrade = "A",
            NumericGrade = 97m,
            GradeDate = new DateTime(2026, 3, 10)
        };
        var created = new GradeRecordDto
        {
            Id = gradeId,
            StudentId = studentId,
            CourseId = courseId,
            LetterGrade = "A",
            NumericGrade = 97m
        };
        _mockService.Setup(s => s.CreateGradeAsync(createDto)).ReturnsAsync(created);

        var result = await _controller.CreateGrade(createDto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(GradesController.GetGrade), createdResult.ActionName);
        Assert.Same(created, createdResult.Value);
    }

    // -------------------------------------------------------------------------
    // PUT /api/grades/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateGrade_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var updateDto = new CreateGradeRecordDto
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            LetterGrade = "A-",
            GradeDate = DateTime.UtcNow
        };
        var updated = new GradeRecordDto { Id = id, LetterGrade = "A-" };
        _mockService.Setup(s => s.UpdateGradeAsync(id, updateDto)).ReturnsAsync(updated);

        var result = await _controller.UpdateGrade(id, updateDto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, okResult.Value);
    }

    [Fact]
    public async Task UpdateGrade_NotFound_ReturnsNotFound()
    {
        _mockService
            .Setup(s => s.UpdateGradeAsync(It.IsAny<Guid>(), It.IsAny<CreateGradeRecordDto>()))
            .ReturnsAsync((GradeRecordDto?)null);

        var result = await _controller.UpdateGrade(Guid.NewGuid(), new CreateGradeRecordDto
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            GradeDate = DateTime.UtcNow
        });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/grades/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteGrade_Exists_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteGradeAsync(id)).ReturnsAsync(true);

        var result = await _controller.DeleteGrade(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteGrade_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteGradeAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.DeleteGrade(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // -------------------------------------------------------------------------
    // GET /api/grades/student/{studentId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGradesByStudent_ReturnsOkWithList()
    {
        var studentId = Guid.NewGuid();
        var grades = new List<GradeRecordDto>
        {
            new() { Id = Guid.NewGuid(), StudentId = studentId, LetterGrade = "B" }
        };
        _mockService.Setup(s => s.GetGradesByStudentAsync(studentId)).ReturnsAsync(grades);

        var result = await _controller.GetGradesByStudent(studentId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(grades, okResult.Value);
    }

    // -------------------------------------------------------------------------
    // GET /api/grades/course/{courseId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGradesByCourse_ReturnsOkWithList()
    {
        var courseId = Guid.NewGuid();
        var grades = new List<GradeRecordDto>
        {
            new() { Id = Guid.NewGuid(), CourseId = courseId, LetterGrade = "A-" }
        };
        _mockService.Setup(s => s.GetGradesByCourseAsync(courseId)).ReturnsAsync(grades);

        var result = await _controller.GetGradesByCourse(courseId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(grades, okResult.Value);
    }
}
