using AutoMapper;
using FluentAssertions;
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

        result.Should().HaveCount(2);
        result.Select(s => s.Code).Should().Contain(new[] { "F26", "S27" });
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

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Code.Should().Be("F26");
    }

    [Fact]
    public async Task GetSemesterByIdAsync_NotFound_ReturnsNull()
    {
        _semesterRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Semester?)null);

        var result = await _service.GetSemesterByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
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

        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveSemesterAsync_NoneActive_ReturnsNull()
    {
        _semesterRepository.Setup(r => r.GetActiveAsync()).ReturnsAsync((Semester?)null);

        var result = await _service.GetActiveSemesterAsync();

        result.Should().BeNull();
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
            .ReturnsAsync((Semester s) =>
            {
                s.Id = Guid.NewGuid();
                return s;
            });
        _semesterRepository
            .Setup(r => r.SetActiveAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => MakeSemester(id: id, isActive: true));

        var result = await _service.CreateSemesterAsync(createDto);

        result.Name.Should().Be("Fall 2026");
        result.Code.Should().Be("F26");
        result.IsActive.Should().BeTrue();

        _semesterRepository.Verify(r => r.CreateAsync(It.Is<Semester>(s =>
            s.Name == "Fall 2026" && s.Code == "F26")), Times.Once);
        _semesterRepository.Verify(r => r.SetActiveAsync(result.Id), Times.Once);
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
        _semesterRepository.Setup(r => r.SetActiveAsync(id)).ReturnsAsync(existing);

        var result = await _service.UpdateSemesterAsync(id, updateDto);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        _semesterRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
        _semesterRepository.Verify(r => r.SetActiveAsync(id), Times.Once);
    }

    [Fact]
    public async Task UpdateSemesterAsync_NotFound_ReturnsNull()
    {
        _semesterRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Semester?)null);

        var result = await _service.UpdateSemesterAsync(Guid.NewGuid(), new UpdateSemesterDto
        {
            Name = "X", Code = "X"
        });

        result.Should().BeNull();
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

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSemesterAsync_HasCourses_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var semester = MakeSemester(id);
        semester.Courses.Add(new Course { Id = Guid.NewGuid(), SemesterId = id });
        _semesterRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(semester);

        await _service.Invoking(s => s.DeleteSemesterAsync(id))
            .Should().ThrowAsync<InvalidOperationException>();

        _semesterRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSemesterAsync_NotFound_ReturnsFalse()
    {
        _semesterRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Semester?)null);

        var result = await _service.DeleteSemesterAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }
}
