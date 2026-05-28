using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class GradeServiceTests
{
    private readonly Mock<IGradeRepository> _gradeRepository = new();
    private readonly GradeService _service;

    public GradeServiceTests()
    {
        var mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _service = new GradeService(_gradeRepository.Object, mapper);
    }

    // -------------------------------------------------------------------------
    // GetAllGradesAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllGradesAsync_ReturnsAllGrades()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var grades = new List<GradeRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Student = new Student { Id = studentId, FirstName = "Sam", LastName = "Student" },
                CourseId = courseId,
                Course = new Course { Id = courseId, Name = "Art" },
                LetterGrade = "A",
                NumericGrade = 95m,
                GradeDate = new DateTime(2026, 1, 15)
            }
        };

        _gradeRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(grades);

        var result = await _service.GetAllGradesAsync();

        var grade = Assert.Single(result);
        Assert.Equal("A", grade.LetterGrade);
        Assert.Equal(95m, grade.NumericGrade);
    }

    // -------------------------------------------------------------------------
    // GetGradeByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGradeByIdAsync_Found_ReturnsMappedDto()
    {
        var gradeId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var grade = new GradeRecord
        {
            Id = gradeId,
            StudentId = studentId,
            Student = new Student { Id = studentId, FirstName = "Sam", LastName = "Student" },
            CourseId = courseId,
            Course = new Course { Id = courseId, Name = "Math" },
            LetterGrade = "B+",
            GradeDate = new DateTime(2026, 2, 1)
        };

        _gradeRepository.Setup(r => r.GetByIdAsync(gradeId)).ReturnsAsync(grade);

        var result = await _service.GetGradeByIdAsync(gradeId);

        Assert.NotNull(result);
        Assert.Equal(gradeId, result!.Id);
        Assert.Equal("B+", result.LetterGrade);
    }

    [Fact]
    public async Task GetGradeByIdAsync_NotFound_ReturnsNull()
    {
        _gradeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GradeRecord?)null);

        var result = await _service.GetGradeByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // GetGradesByStudentAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGradesByStudentAsync_ReturnsMappedList()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var grades = new List<GradeRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Student = new Student { Id = studentId, FirstName = "Sam", LastName = "Student" },
                CourseId = courseId,
                Course = new Course { Id = courseId, Name = "Science" },
                LetterGrade = "A-",
                GradeDate = DateTime.UtcNow
            }
        };

        _gradeRepository.Setup(r => r.GetByStudentIdAsync(studentId)).ReturnsAsync(grades);

        var result = await _service.GetGradesByStudentAsync(studentId);

        var grade = Assert.Single(result);
        Assert.Equal(studentId, grade.StudentId);
    }

    // -------------------------------------------------------------------------
    // GetGradesByCourseAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGradesByCourseAsync_ReturnsMappedList()
    {
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var grades = new List<GradeRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Student = new Student { Id = studentId, FirstName = "Sam", LastName = "Student" },
                CourseId = courseId,
                Course = new Course { Id = courseId, Name = "History" },
                LetterGrade = "C+",
                GradeDate = DateTime.UtcNow
            }
        };

        _gradeRepository.Setup(r => r.GetByCourseIdAsync(courseId)).ReturnsAsync(grades);

        var result = await _service.GetGradesByCourseAsync(courseId);

        var grade = Assert.Single(result);
        Assert.Equal(courseId, grade.CourseId);
    }

    // -------------------------------------------------------------------------
    // CreateGradeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateGradeAsync_CreatesAndReturnsMappedDto()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var gradeDate = new DateTime(2026, 3, 10);
        var createDto = new CreateGradeRecordDto
        {
            StudentId = studentId,
            CourseId = courseId,
            LetterGrade = "A",
            NumericGrade = 97m,
            GradePoints = 4.0m,
            GradeDate = gradeDate
        };

        _gradeRepository
            .Setup(r => r.CreateAsync(It.IsAny<GradeRecord>()))
            .ReturnsAsync((GradeRecord g) =>
            {
                g.Student = new Student { Id = studentId, FirstName = "Sam", LastName = "Student" };
                g.Course = new Course { Id = courseId, Name = "English" };
                return g;
            });

        var result = await _service.CreateGradeAsync(createDto);

        Assert.Equal(studentId, result.StudentId);
        Assert.Equal(courseId, result.CourseId);
        Assert.Equal("A", result.LetterGrade);
        Assert.Equal(97m, result.NumericGrade);

        _gradeRepository.Verify(r => r.CreateAsync(It.Is<GradeRecord>(g =>
            g.StudentId == studentId &&
            g.CourseId == courseId &&
            g.LetterGrade == "A" &&
            g.NumericGrade == 97m)), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateGradeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateGradeAsync_Found_UpdatesAndReturnsMappedDto()
    {
        var gradeId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var existing = new GradeRecord
        {
            Id = gradeId,
            StudentId = studentId,
            Student = new Student { Id = studentId, FirstName = "Sam", LastName = "Student" },
            CourseId = courseId,
            Course = new Course { Id = courseId, Name = "PE" },
            LetterGrade = "B",
            GradeDate = new DateTime(2026, 1, 1)
        };
        var updateDto = new CreateGradeRecordDto
        {
            StudentId = studentId,
            CourseId = courseId,
            LetterGrade = "A",
            GradeDate = new DateTime(2026, 1, 15)
        };

        _gradeRepository.Setup(r => r.GetByIdAsync(gradeId)).ReturnsAsync(existing);
        _gradeRepository.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var result = await _service.UpdateGradeAsync(gradeId, updateDto);

        Assert.NotNull(result);
        Assert.Equal(gradeId, result!.Id);
        Assert.Equal("A", result.LetterGrade);

        _gradeRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task UpdateGradeAsync_NotFound_ReturnsNull()
    {
        _gradeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GradeRecord?)null);

        var result = await _service.UpdateGradeAsync(Guid.NewGuid(), new CreateGradeRecordDto
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            GradeDate = DateTime.UtcNow
        });

        Assert.Null(result);
        _gradeRepository.Verify(r => r.UpdateAsync(It.IsAny<GradeRecord>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // DeleteGradeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteGradeAsync_Exists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _gradeRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteGradeAsync(id);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteGradeAsync_NotFound_ReturnsFalse()
    {
        _gradeRepository.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _service.DeleteGradeAsync(Guid.NewGuid());

        Assert.False(result);
    }
}
