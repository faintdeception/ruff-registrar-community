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

public class SemestersControllerTests
{
    private readonly Mock<ISemesterService> _mockService = new();
    private readonly SemestersController _controller;

    public SemestersControllerTests()
    {
        _controller = new SemestersController(
            _mockService.Object,
            NullLogger<SemestersController>.Instance);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private static SemesterDto MakeDto(Guid? id = null, string name = "Fall 2026", string code = "F26", bool isActive = true)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Code = code,
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 12, 15),
            IsActive = isActive
        };

    // -------------------------------------------------------------------------
    // GET /api/semesters
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSemesters_ReturnsOkWithList()
    {
        var semesters = new List<SemesterDto> { MakeDto(), MakeDto(name: "Spring 2027", code: "S27") };
        _mockService.Setup(s => s.GetAllSemestersAsync()).ReturnsAsync(semesters);

        var result = await _controller.GetSemesters();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(semesters);
    }

    // -------------------------------------------------------------------------
    // GET /api/semesters/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSemester_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id: id);
        _mockService.Setup(s => s.GetSemesterByIdAsync(id)).ReturnsAsync(dto);

        var result = await _controller.GetSemester(id);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetSemester_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetSemesterByIdAsync(It.IsAny<Guid>())).ReturnsAsync((SemesterDto?)null);

        var result = await _controller.GetSemester(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -------------------------------------------------------------------------
    // GET /api/semesters/active
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetActiveSemester_Found_ReturnsOk()
    {
        var dto = MakeDto(isActive: true);
        _mockService.Setup(s => s.GetActiveSemesterAsync()).ReturnsAsync(dto);

        var result = await _controller.GetActiveSemester();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetActiveSemester_NoneActive_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetActiveSemesterAsync()).ReturnsAsync((SemesterDto?)null);

        var result = await _controller.GetActiveSemester();

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // -------------------------------------------------------------------------
    // POST /api/semesters (Administrator only)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateSemester_ReturnsCreatedAtAction()
    {
        var id = Guid.NewGuid();
        var createDto = new CreateSemesterDto
        {
            Name = "Fall 2026",
            Code = "F26",
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 12, 15),
            RegistrationStartDate = new DateTime(2026, 7, 1),
            RegistrationEndDate = new DateTime(2026, 8, 31),
            IsActive = true
        };
        var created = MakeDto(id: id);
        _mockService.Setup(s => s.CreateSemesterAsync(createDto)).ReturnsAsync(created);

        var result = await _controller.CreateSemester(createDto);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(SemestersController.GetSemester));
        createdResult.Value.Should().BeEquivalentTo(created);
    }

    // -------------------------------------------------------------------------
    // PUT /api/semesters/{id} (Administrator only)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateSemester_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateSemesterDto
        {
            Name = "Fall 2026 Updated",
            Code = "F26U",
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 12, 20),
            RegistrationStartDate = new DateTime(2026, 7, 1),
            RegistrationEndDate = new DateTime(2026, 8, 31),
            IsActive = true
        };
        var updated = MakeDto(id: id, name: "Fall 2026 Updated", code: "F26U");
        _mockService.Setup(s => s.UpdateSemesterAsync(id, updateDto)).ReturnsAsync(updated);

        var result = await _controller.UpdateSemester(id, updateDto);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(updated);
    }

    [Fact]
    public async Task UpdateSemester_NotFound_ReturnsNotFound()
    {
        _mockService
            .Setup(s => s.UpdateSemesterAsync(It.IsAny<Guid>(), It.IsAny<UpdateSemesterDto>()))
            .ReturnsAsync((SemesterDto?)null);

        var result = await _controller.UpdateSemester(Guid.NewGuid(), new UpdateSemesterDto
        {
            Name = "X", Code = "X"
        });

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -------------------------------------------------------------------------
    // DELETE /api/semesters/{id} (Administrator only)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteSemester_Exists_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteSemesterAsync(id)).ReturnsAsync(true);

        var result = await _controller.DeleteSemester(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteSemester_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteSemesterAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.DeleteSemester(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }
}
