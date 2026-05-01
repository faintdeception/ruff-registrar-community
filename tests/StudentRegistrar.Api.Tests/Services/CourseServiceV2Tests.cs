using AutoMapper;
using FluentAssertions;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class CourseServiceV2Tests
{
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ICourseInstructorRepository> _courseInstructorRepository = new();
    private readonly Mock<IAccountHolderRepository> _accountHolderRepository = new();
    private readonly Mock<IEnrollmentRepository> _enrollmentRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IEducatorRepository> _educatorRepository = new();
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<IKeycloakService> _keycloakService = new();
    private readonly CourseServiceV2 _service;

    public CourseServiceV2Tests()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _service = new CourseServiceV2(
            _courseRepository.Object,
            _courseInstructorRepository.Object,
            _accountHolderRepository.Object,
            _enrollmentRepository.Object,
            _paymentRepository.Object,
            _educatorRepository.Object,
            _roomRepository.Object,
            _keycloakService.Object,
            mapperConfig.CreateMapper());
    }

    [Fact]
    public async Task EnrollStudentAsync_Should_Enroll_And_Record_Payment_For_Paid_Course()
    {
        var accountHolderId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();
        var keycloakUserId = "keycloak-parent-1";
        var accountHolder = new AccountHolder
        {
            Id = accountHolderId,
            KeycloakUserId = keycloakUserId,
            FirstName = "Pat",
            LastName = "Parent",
            EmailAddress = "pat.parent@example.com",
            Students = new List<Student>
            {
                new()
                {
                    Id = studentId,
                    AccountHolderId = accountHolderId,
                    FirstName = "Sam",
                    LastName = "Student"
                }
            }
        };
        var course = new Course
        {
            Id = courseId,
            SemesterId = semesterId,
            Name = "Art Studio",
            MaxCapacity = 12,
            Fee = 45m,
            Enrollments = new List<Enrollment>()
        };

        _accountHolderRepository
            .Setup(r => r.GetByKeycloakUserIdAsync(keycloakUserId))
            .ReturnsAsync(accountHolder);

        _courseRepository
            .Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync(course);

        _enrollmentRepository
            .Setup(r => r.HasEnrollmentAsync(studentId, courseId, null))
            .ReturnsAsync(false);

        _enrollmentRepository
            .Setup(r => r.CreateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment enrollment) => enrollment);

        _enrollmentRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment enrollment) => enrollment);

        _paymentRepository
            .Setup(r => r.CreateAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment payment) => payment);

        var result = await _service.EnrollStudentAsync(courseId, new CreateCourseEnrollmentDto
        {
            StudentId = studentId,
            PaymentMethod = PaymentMethod.CreditCard
        }, keycloakUserId);

        result.StudentId.Should().Be(studentId.ToString());
        result.CourseId.Should().Be(courseId.ToString());
        result.EnrollmentType.Should().Be(nameof(EnrollmentType.Enrolled));
        result.FeeAmount.Should().Be(45m);
        result.AmountPaid.Should().Be(45m);
        result.PaymentStatus.Should().Be(nameof(PaymentStatus.Paid));
        result.PaymentId.Should().NotBeNullOrWhiteSpace();

        _paymentRepository.Verify(r => r.CreateAsync(It.Is<Payment>(p =>
            p.AccountHolderId == accountHolderId &&
            p.EnrollmentId.HasValue &&
            p.Amount == 45m &&
            p.PaymentMethod == PaymentMethod.CreditCard &&
            p.PaymentType == PaymentType.CourseFee)), Times.Once);
    }

    [Fact]
    public async Task EnrollStudentAsync_Should_Reject_Student_From_Another_Account()
    {
        var accountHolder = new AccountHolder
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = "keycloak-parent-2",
            FirstName = "Pat",
            LastName = "Parent",
            EmailAddress = "pat.parent@example.com",
            Students = new List<Student>()
        };

        _accountHolderRepository
            .Setup(r => r.GetByKeycloakUserIdAsync(accountHolder.KeycloakUserId))
            .ReturnsAsync(accountHolder);

        var act = () => _service.EnrollStudentAsync(Guid.NewGuid(), new CreateCourseEnrollmentDto
        {
            StudentId = Guid.NewGuid()
        }, accountHolder.KeycloakUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Student does not belong to the current account.");

        _enrollmentRepository.Verify(r => r.CreateAsync(It.IsAny<Enrollment>()), Times.Never);
        _paymentRepository.Verify(r => r.CreateAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
            public async Task AddInstructorAsync_Should_Grant_Educator_Role_And_Create_Educator_When_Instructor_Is_AccountHolder()
    {
        var courseId = Guid.NewGuid();
        var accountHolderId = Guid.NewGuid();
        var keycloakUserId = "keycloak-parent-educator-1";
        var accountHolder = new AccountHolder
        {
            Id = accountHolderId,
            FirstName = "Parent",
            LastName = "Educator",
            EmailAddress = "parent.educator@example.com",
            MobilePhone = "555-0101",
            KeycloakUserId = keycloakUserId
        };
        var request = new CreateCourseInstructorDto
        {
            CourseId = courseId,
            AccountHolderId = accountHolderId,
            FirstName = "Ignored",
            LastName = "Input",
            Email = "ignored@example.com",
            IsPrimary = true
        };

        _accountHolderRepository
            .Setup(r => r.GetByIdAsync(accountHolderId))
            .ReturnsAsync(accountHolder);

        _keycloakService
            .Setup(s => s.GetUserIdByEmailAsync(accountHolder.EmailAddress))
            .ReturnsAsync(keycloakUserId);

        _educatorRepository
            .Setup(r => r.GetByAccountHolderIdAsync(accountHolderId))
            .ReturnsAsync((Educator?)null);

        _educatorRepository
            .Setup(r => r.CreateAsync(It.IsAny<Educator>()))
            .ReturnsAsync((Educator educator) => educator);

        _courseInstructorRepository
            .Setup(r => r.CreateAsync(It.IsAny<CourseInstructor>()))
            .ReturnsAsync((CourseInstructor instructor) => instructor);

        var result = await _service.AddInstructorAsync(request);

        result.AccountHolderId.Should().Be(accountHolderId);
        result.FirstName.Should().Be(accountHolder.FirstName);
        result.LastName.Should().Be(accountHolder.LastName);
        result.Email.Should().Be(accountHolder.EmailAddress);

        _keycloakService.Verify(s => s.UpdateUserRoleAsync(keycloakUserId, UserRole.Educator), Times.Once);
        _educatorRepository.Verify(r => r.CreateAsync(It.Is<Educator>(e =>
            e.AccountHolderId == accountHolderId &&
            e.FirstName == accountHolder.FirstName &&
            e.LastName == accountHolder.LastName &&
            e.Email == accountHolder.EmailAddress &&
            e.Phone == accountHolder.MobilePhone &&
            e.KeycloakUserId == keycloakUserId &&
            e.IsActive)), Times.Once);
        _courseInstructorRepository.Verify(r => r.CreateAsync(It.Is<CourseInstructor>(i =>
            i.CourseId == courseId &&
            i.AccountHolderId == accountHolderId &&
            i.FirstName == accountHolder.FirstName &&
            i.LastName == accountHolder.LastName &&
            i.Email == accountHolder.EmailAddress &&
            i.IsPrimary)), Times.Once);
    }

    [Fact]
    public async Task AddInstructorAsync_Should_Update_Existing_Educator_When_Instructor_Is_AccountHolder()
    {
        var courseId = Guid.NewGuid();
        var accountHolderId = Guid.NewGuid();
        var keycloakUserId = "keycloak-parent-educator-2";
        var accountHolder = new AccountHolder
        {
            Id = accountHolderId,
            FirstName = "Updated",
            LastName = "Parent",
            EmailAddress = "updated.parent@example.com",
            HomePhone = "555-0202",
            KeycloakUserId = keycloakUserId
        };
        var existingEducator = new Educator
        {
            Id = Guid.NewGuid(),
            AccountHolderId = accountHolderId,
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com",
            IsActive = false
        };
        var request = new CreateCourseInstructorDto
        {
            CourseId = courseId,
            AccountHolderId = accountHolderId,
            IsPrimary = false
        };

        _accountHolderRepository
            .Setup(r => r.GetByIdAsync(accountHolderId))
            .ReturnsAsync(accountHolder);

        _keycloakService
            .Setup(s => s.GetUserIdByEmailAsync(accountHolder.EmailAddress))
            .ReturnsAsync(keycloakUserId);

        _educatorRepository
            .Setup(r => r.GetByAccountHolderIdAsync(accountHolderId))
            .ReturnsAsync(existingEducator);

        _educatorRepository
            .Setup(r => r.UpdateAsync(existingEducator))
            .ReturnsAsync(existingEducator);

        _courseInstructorRepository
            .Setup(r => r.CreateAsync(It.IsAny<CourseInstructor>()))
            .ReturnsAsync((CourseInstructor instructor) => instructor);

        var result = await _service.AddInstructorAsync(request);

        result.AccountHolderId.Should().Be(accountHolderId);

        _keycloakService.Verify(s => s.UpdateUserRoleAsync(keycloakUserId, UserRole.Educator), Times.Once);
        _educatorRepository.Verify(r => r.CreateAsync(It.IsAny<Educator>()), Times.Never);
        _educatorRepository.Verify(r => r.UpdateAsync(It.Is<Educator>(e =>
            e.Id == existingEducator.Id &&
            e.AccountHolderId == accountHolderId &&
            e.FirstName == accountHolder.FirstName &&
            e.LastName == accountHolder.LastName &&
            e.Email == accountHolder.EmailAddress &&
            e.Phone == accountHolder.HomePhone &&
            e.KeycloakUserId == keycloakUserId &&
            e.IsActive)), Times.Once);
    }
}
