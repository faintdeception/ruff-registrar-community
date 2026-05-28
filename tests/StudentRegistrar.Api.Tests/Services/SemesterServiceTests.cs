using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class SemesterServiceTests
{
    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly SemesterService _service;

    public SemesterServiceTests()
    {
        var mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _service = new SemesterService(_semesterRepository.Object, mapper);
    }

    private static Semester MakeSemester(Guid? id = null, string name = "Fall 2026", string code = "F26", bool isActive = true)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = name,
            Code = code,
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 12, 15),
            RegistrationStartDate = new DateTime(2026, 7, 1),
            RegistrationEndDate = new DateTime(2026, 8, 31),
            IsActive = isActive
        };

    // -------------------------------------------------------------------------
    // GetAllSemestersAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllSemestersAsync_ReturnsAllSemesters()
    {
        var semesters = new List<Semester> { MakeSemester(), MakeSemester(name: "Spring 2027", code: "S27") };
        _semesterRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(semesters);

        var result = await _service.GetAllSemestersAsync();

        Assert.Equal(2, result.Count());
        Assert.Contains("F26", result.Select(s => s.Code));
        Assert.Contains("S27", result.Select(s => s.Code));
    }

    // -------------------------------------------------------------------------
    // GetSemesterByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSemesterByIdAsync_Found_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var semester = MakeSemester(id: id, code: "F26");
        _semesterRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(semester);

        var result = await _service.GetSemesterByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("F26", result.Code);
    }

    [Fact]
    public async Task GetSemesterByIdAsync_NotFound_ReturnsNull()
    {
        _semesterRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Semester?)null);

        var result = await _service.GetSemesterByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // GetActiveSemesterAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetActiveSemesterAsync_Found_ReturnsActiveSemester()
    {
        var active = MakeSemester(isActive: true);
        _semesterRepository.Setup(r => r.GetActiveAsync()).ReturnsAsync(active);

        var result = await _service.GetActiveSemesterAsync();

        Assert.NotNull(result);
        Assert.True(result!.IsActive);
    }

    [Fact]
    public async Task GetActiveSemesterAsync_NoneActive_ReturnsNull()
    {
        _semesterRepository.Setup(r => r.GetActiveAsync()).ReturnsAsync((Semester?)null);

        var result = await _service.GetActiveSemesterAsync();

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // CreateSemesterAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateSemesterAsync_CreatesAndReturnsMappedDto()
    {
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

        _semesterRepository
            .Setup(r => r.CreateAsync(It.IsAny<Semester>()))
            .ReturnsAsync((Semester s) => s);

        var result = await _service.CreateSemesterAsync(createDto);

        Assert.Equal("Fall 2026", result.Name);
        Assert.Equal("F26", result.Code);
        Assert.True(result.IsActive);

        _semesterRepository.Verify(r => r.CreateAsync(It.Is<Semester>(s =>
            s.Name == "Fall 2026" && s.Code == "F26")), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateSemesterAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateSemesterAsync_Found_UpdatesAndReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var existing = MakeSemester(id: id, name: "Fall 2026", code: "F26");
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

        _semesterRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _semesterRepository.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var result = await _service.UpdateSemesterAsync(id, updateDto);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        _semesterRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task UpdateSemesterAsync_NotFound_ReturnsNull()
    {
        _semesterRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Semester?)null);

        var result = await _service.UpdateSemesterAsync(Guid.NewGuid(), new UpdateSemesterDto
        {
            Name = "X", Code = "X"
        });

        Assert.Null(result);
        _semesterRepository.Verify(r => r.UpdateAsync(It.IsAny<Semester>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // DeleteSemesterAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteSemesterAsync_Exists_NoCourses_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var semester = MakeSemester(id);
        // Courses collection is empty by default
        _semesterRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(semester);
        _semesterRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteSemesterAsync(id);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteSemesterAsync_HasCourses_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var semester = MakeSemester(id);
        semester.Courses.Add(new Course { Id = Guid.NewGuid(), SemesterId = id });
        _semesterRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(semester);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteSemesterAsync(id));

        _semesterRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSemesterAsync_NotFound_ReturnsFalse()
    {
        _semesterRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Semester?)null);

        var result = await _service.DeleteSemesterAsync(Guid.NewGuid());

        Assert.False(result);
    }
}
