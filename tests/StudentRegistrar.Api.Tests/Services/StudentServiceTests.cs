using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class StudentServiceTests
{
    private readonly Mock<IStudentRepository> _studentRepository = new();
    private readonly Mock<IAccountHolderRepository> _accountHolderRepository = new();
    private readonly Mock<IEnrollmentRepository> _enrollmentRepository = new();
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        var mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _service = new StudentService(
            _studentRepository.Object,
            _accountHolderRepository.Object,
            _enrollmentRepository.Object,
            mapper);
    }

    private static Student MakeStudent(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        AccountHolderId = Guid.NewGuid(),
        FirstName = "Sam",
        LastName = "Student"
    };

    // -------------------------------------------------------------------------
    // DeleteStudentAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteStudentAsync_NoEnrollments_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _enrollmentRepository.Setup(r => r.GetByStudentAsync(id)).ReturnsAsync(new List<Enrollment>());
        _studentRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteStudentAsync(id);

        Assert.True(result);
        _studentRepository.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteStudentAsync_OnlyWithdrawnEnrollments_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _enrollmentRepository.Setup(r => r.GetByStudentAsync(id)).ReturnsAsync(new List<Enrollment>
        {
            new() { StudentId = id, EnrollmentType = EnrollmentType.Withdrawn },
            new() { StudentId = id, EnrollmentType = EnrollmentType.Cancelled }
        });
        _studentRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteStudentAsync(id);

        Assert.True(result);
        _studentRepository.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteStudentAsync_HasActiveEnrolledStudent_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _enrollmentRepository.Setup(r => r.GetByStudentAsync(id)).ReturnsAsync(new List<Enrollment>
        {
            new() { StudentId = id, EnrollmentType = EnrollmentType.Enrolled }
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteStudentAsync(id));
        Assert.Contains("active enrollments", exception.Message);

        _studentRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteStudentAsync_HasWaitlistedStudent_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _enrollmentRepository.Setup(r => r.GetByStudentAsync(id)).ReturnsAsync(new List<Enrollment>
        {
            new() { StudentId = id, EnrollmentType = EnrollmentType.Waitlisted }
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteStudentAsync(id));
        Assert.Contains("active enrollments", exception.Message);

        _studentRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteStudentAsync_MixedEnrollments_ActivePresent_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _enrollmentRepository.Setup(r => r.GetByStudentAsync(id)).ReturnsAsync(new List<Enrollment>
        {
            new() { StudentId = id, EnrollmentType = EnrollmentType.Withdrawn },
            new() { StudentId = id, EnrollmentType = EnrollmentType.Enrolled }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteStudentAsync(id));

        _studentRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
