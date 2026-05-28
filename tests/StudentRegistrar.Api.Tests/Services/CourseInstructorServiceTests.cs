using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class CourseInstructorServiceTests
{
    private readonly Mock<ICourseInstructorRepository> _repo = new();
    private readonly CourseInstructorService _service;

    public CourseInstructorServiceTests()
    {
        var mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _service = new CourseInstructorService(_repo.Object, mapper);
    }

    private static CourseInstructor MakeInstructor(Guid? id = null, Guid? courseId = null,
        string firstName = "Alice", string lastName = "Smith", bool isPrimary = false)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CourseId = courseId ?? Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = "alice@example.com",
            IsPrimary = isPrimary
        };

    // -------------------------------------------------------------------------
    // GetAllCourseInstructorsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllCourseInstructorsAsync_ReturnsAllInstructors()
    {
        var instructors = new List<CourseInstructor> { MakeInstructor(), MakeInstructor(firstName: "Bob", lastName: "Jones") };
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(instructors);

        var result = await _service.GetAllCourseInstructorsAsync();

        Assert.Equal(2, result.Count());
        Assert.Contains("Alice", result.Select(i => i.FirstName));
        Assert.Contains("Bob", result.Select(i => i.FirstName));
    }

    // -------------------------------------------------------------------------
    // GetCourseInstructorByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCourseInstructorByIdAsync_Found_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var instructor = MakeInstructor(id: id);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(instructor);

        var result = await _service.GetCourseInstructorByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("Alice", result.FirstName);
    }

    [Fact]
    public async Task GetCourseInstructorByIdAsync_NotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CourseInstructor?)null);

        var result = await _service.GetCourseInstructorByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // GetCourseInstructorsByCourseIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCourseInstructorsByCourseIdAsync_ReturnsMappedList()
    {
        var courseId = Guid.NewGuid();
        var instructors = new List<CourseInstructor> { MakeInstructor(courseId: courseId) };
        _repo.Setup(r => r.GetByCourseIdAsync(courseId)).ReturnsAsync(instructors);

        var result = await _service.GetCourseInstructorsByCourseIdAsync(courseId);

        var instructor = Assert.Single(result);
        Assert.Equal(courseId, instructor.CourseId);
    }

    // -------------------------------------------------------------------------
    // CreateCourseInstructorAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateCourseInstructorAsync_CreatesAndReturnsMappedDto()
    {
        var courseId = Guid.NewGuid();
        var createDto = new CreateCourseInstructorDto
        {
            CourseId = courseId,
            FirstName = "Carol",
            LastName = "White",
            Email = "carol@example.com",
            IsPrimary = true
        };

        _repo.Setup(r => r.CreateAsync(It.IsAny<CourseInstructor>()))
            .ReturnsAsync((CourseInstructor ci) => ci);

        var result = await _service.CreateCourseInstructorAsync(createDto);

        Assert.Equal("Carol", result.FirstName);
        Assert.Equal("White", result.LastName);
        Assert.True(result.IsPrimary);

        _repo.Verify(r => r.CreateAsync(It.Is<CourseInstructor>(ci =>
            ci.FirstName == "Carol" && ci.CourseId == courseId)), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateCourseInstructorAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateCourseInstructorAsync_Found_UpdatesAndReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var existing = MakeInstructor(id: id, firstName: "Alice", lastName: "Smith");
        var updateDto = new UpdateCourseInstructorDto
        {
            FirstName = "Alice",
            LastName = "Updated",
            IsPrimary = true
        };

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var result = await _service.UpdateCourseInstructorAsync(id, updateDto);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        _repo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseInstructorAsync_NotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CourseInstructor?)null);

        var result = await _service.UpdateCourseInstructorAsync(Guid.NewGuid(), new UpdateCourseInstructorDto
        {
            FirstName = "X", LastName = "Y"
        });

        Assert.Null(result);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<CourseInstructor>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // DeleteCourseInstructorAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteCourseInstructorAsync_Exists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteCourseInstructorAsync(id);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteCourseInstructorAsync_NotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _service.DeleteCourseInstructorAsync(Guid.NewGuid());

        Assert.False(result);
    }
}
