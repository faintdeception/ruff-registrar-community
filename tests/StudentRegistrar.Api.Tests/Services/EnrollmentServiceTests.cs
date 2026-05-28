using Moq;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _enrollmentRepo = new();
    private readonly Mock<IAccountHolderRepository> _accountHolderRepo = new();
    private readonly Mock<ICourseRepository> _courseRepo = new();
    private readonly EnrollmentService _service;

    public EnrollmentServiceTests()
    {
        _service = new EnrollmentService(
            _enrollmentRepo.Object,
            _accountHolderRepo.Object,
            _courseRepo.Object);
    }

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_NoFilters_ReturnsAllEnrollments()
    {
        var enrollments = new List<Enrollment>
        {
            MakeEnrollment(Guid.NewGuid(), EnrollmentType.Enrolled),
            MakeEnrollment(Guid.NewGuid(), EnrollmentType.Waitlisted)
        };
        _enrollmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithTypeFilter_FiltersLocally()
    {
        var enrolled = MakeEnrollment(Guid.NewGuid(), EnrollmentType.Enrolled);
        var waitlisted = MakeEnrollment(Guid.NewGuid(), EnrollmentType.Waitlisted);
        _enrollmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Enrollment> { enrolled, waitlisted });

        var result = await _service.GetAllAsync(type: EnrollmentType.Enrolled);

        var enrollment = Assert.Single(result);
        Assert.Equal("Enrolled", enrollment.EnrollmentType);
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var enrollment = MakeEnrollment(id, EnrollmentType.Enrolled);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);

        var result = await _service.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id.ToString(), result!.Id);
        Assert.Equal("Enrolled", result.EnrollmentType);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _enrollmentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Enrollment?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // WithdrawAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task WithdrawAsync_Enrolled_SetsWithdrawn()
    {
        var id = Guid.NewGuid();
        var enrollment = MakeEnrollment(id, EnrollmentType.Enrolled);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);
        _enrollmentRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment e) => e);

        var result = await _service.WithdrawAsync(id, keycloakUserId: null, reason: "Schedule conflict");

        Assert.Equal("Withdrawn", result.EnrollmentType);
        Assert.Equal("Schedule conflict", result.Notes);
        _enrollmentRepo.Verify(r => r.UpdateAsync(It.Is<Enrollment>(e =>
            e.EnrollmentType == EnrollmentType.Withdrawn)), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_Waitlisted_SetsWithdrawn()
    {
        var id = Guid.NewGuid();
        var enrollment = MakeEnrollment(id, EnrollmentType.Waitlisted);
        enrollment.WaitlistPosition = 2;
        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);
        _enrollmentRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment e) => e);

        var result = await _service.WithdrawAsync(id, keycloakUserId: null, reason: null);

        Assert.Equal("Withdrawn", result.EnrollmentType);
    }

    [Fact]
    public async Task WithdrawAsync_AlreadyWithdrawn_ThrowsInvalidOperation()
    {
        var id = Guid.NewGuid();
        var enrollment = MakeEnrollment(id, EnrollmentType.Withdrawn);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.WithdrawAsync(id, null, null));
    }

    [Fact]
    public async Task WithdrawAsync_NotFound_ThrowsKeyNotFound()
    {
        _enrollmentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Enrollment?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.WithdrawAsync(Guid.NewGuid(), null, null));
    }

    [Fact]
    public async Task WithdrawAsync_MemberOwnsStudent_Succeeds()
    {
        var keycloakUserId = "kc-user-1";
        var studentId = Guid.NewGuid();
        var id = Guid.NewGuid();

        var enrollment = MakeEnrollment(id, EnrollmentType.Enrolled);
        enrollment.StudentId = studentId;

        var accountHolder = new AccountHolder
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            Students = new List<Student> { new() { Id = studentId } }
        };

        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);
        _accountHolderRepo.Setup(r => r.GetByKeycloakUserIdAsync(keycloakUserId)).ReturnsAsync(accountHolder);
        _enrollmentRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment e) => e);

        var result = await _service.WithdrawAsync(id, keycloakUserId, reason: null);

        Assert.Equal("Withdrawn", result.EnrollmentType);
    }

    [Fact]
    public async Task WithdrawAsync_MemberDoesNotOwnStudent_ThrowsUnauthorized()
    {
        var keycloakUserId = "kc-user-1";
        var id = Guid.NewGuid();
        var enrollment = MakeEnrollment(id, EnrollmentType.Enrolled);
        enrollment.StudentId = Guid.NewGuid(); // different student

        var accountHolder = new AccountHolder
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            Students = new List<Student> { new() { Id = Guid.NewGuid() } } // different id
        };

        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);
        _accountHolderRepo.Setup(r => r.GetByKeycloakUserIdAsync(keycloakUserId)).ReturnsAsync(accountHolder);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.WithdrawAsync(id, keycloakUserId, null));
    }

    // -------------------------------------------------------------------------
    // CancelAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CancelAsync_Enrolled_SetsCancelled()
    {
        var id = Guid.NewGuid();
        var enrollment = MakeEnrollment(id, EnrollmentType.Enrolled);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);
        _enrollmentRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment e) => e);

        var result = await _service.CancelAsync(id);

        Assert.Equal("Cancelled", result.EnrollmentType);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ThrowsInvalidOperation()
    {
        var id = Guid.NewGuid();
        var enrollment = MakeEnrollment(id, EnrollmentType.Cancelled);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelAsync(id));
    }

    // -------------------------------------------------------------------------
    // PromoteFromWaitlistAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PromoteFromWaitlistAsync_Waitlisted_SetsEnrolled()
    {
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var enrollment = MakeEnrollment(id, EnrollmentType.Waitlisted);
        enrollment.CourseId = courseId;
        enrollment.WaitlistPosition = 1;

        var course = new Course
        {
            Id = courseId,
            Name = "Algebra I",
            Fee = 150m,
            MaxCapacity = 10,
            Enrollments = new List<Enrollment>()
        };

        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);
        _courseRepo.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(course);
        _enrollmentRepo.Setup(r => r.GetWaitlistAsync(courseId)).ReturnsAsync(new List<Enrollment>());
        _enrollmentRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment e) => e);

        var result = await _service.PromoteFromWaitlistAsync(id);

        Assert.Equal("Enrolled", result.EnrollmentType);
        Assert.Equal(150m, result.FeeAmount);
        Assert.Equal("Pending", result.PaymentStatus);
        Assert.Null(result.WaitlistPosition);
    }

    [Fact]
    public async Task PromoteFromWaitlistAsync_NotWaitlisted_ThrowsInvalidOperation()
    {
        var id = Guid.NewGuid();
        var enrollment = MakeEnrollment(id, EnrollmentType.Enrolled);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PromoteFromWaitlistAsync(id));
    }

    [Fact]
    public async Task PromoteFromWaitlistAsync_ShiftsRemainingWaitlistPositions()
    {
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var enrollment = MakeEnrollment(id, EnrollmentType.Waitlisted);
        enrollment.CourseId = courseId;
        enrollment.WaitlistPosition = 1;

        var course = new Course
        {
            Id = courseId,
            Fee = 0m,
            MaxCapacity = 5,
            Enrollments = new List<Enrollment>()
        };

        var waiter2 = MakeEnrollment(Guid.NewGuid(), EnrollmentType.Waitlisted);
        waiter2.CourseId = courseId;
        waiter2.WaitlistPosition = 2;

        var waiter3 = MakeEnrollment(Guid.NewGuid(), EnrollmentType.Waitlisted);
        waiter3.CourseId = courseId;
        waiter3.WaitlistPosition = 3;

        _enrollmentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(enrollment);
        _courseRepo.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(course);
        _enrollmentRepo.Setup(r => r.GetWaitlistAsync(courseId))
            .ReturnsAsync(new List<Enrollment> { waiter2, waiter3 });
        _enrollmentRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment e) => e);

        await _service.PromoteFromWaitlistAsync(id);

        // Both remaining waiters should have had UpdateAsync called with decremented positions.
        _enrollmentRepo.Verify(r => r.UpdateAsync(It.Is<Enrollment>(
            e => e.Id == waiter2.Id && e.WaitlistPosition == 1)), Times.Once);
        _enrollmentRepo.Verify(r => r.UpdateAsync(It.Is<Enrollment>(
            e => e.Id == waiter3.Id && e.WaitlistPosition == 2)), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Enrollment MakeEnrollment(Guid id, EnrollmentType type)
    {
        return new Enrollment
        {
            Id = id,
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            EnrollmentType = type,
            EnrollmentDate = DateTime.UtcNow,
            FeeAmount = 100m,
            AmountPaid = 0m,
            PaymentStatus = PaymentStatus.Pending,
            EnrollmentInfoJson = "{}"
        };
    }
}
