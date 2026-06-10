using AutoMapper;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

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
    private readonly Mock<ITenantStripePaymentService> _tenantStripePaymentService = new();
    private readonly CourseServiceV2 _service;

    public CourseServiceV2Tests()
    {
        var mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();
        _service = new CourseServiceV2(
            _courseRepository.Object,
            _courseInstructorRepository.Object,
            _accountHolderRepository.Object,
            _enrollmentRepository.Object,
            _paymentRepository.Object,
            _educatorRepository.Object,
            _roomRepository.Object,
            _keycloakService.Object,
                _tenantStripePaymentService.Object,
            mapper);
    }

    [Fact]
            public async Task EnrollStudentAsync_Should_Start_Checkout_For_Paid_Course()
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
            .Setup(r => r.HasEnrollmentAsync(studentId, courseId, EnrollmentType.Enrolled))
            .ReturnsAsync(false);

        _enrollmentRepository
            .Setup(r => r.HasEnrollmentAsync(studentId, courseId, EnrollmentType.Waitlisted))
            .ReturnsAsync(false);

        _enrollmentRepository
            .Setup(r => r.CreateAsync(It.IsAny<Enrollment>()))
            .ReturnsAsync((Enrollment enrollment) => enrollment);

        _enrollmentRepository
            .Setup(r => r.GetByStudentIdAsync(studentId, semesterId))
            .ReturnsAsync(Array.Empty<Enrollment>());

        _tenantStripePaymentService
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<CreateTenantStripeCheckoutSessionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantStripeCheckoutSessionDto
            {
                PaymentId = Guid.NewGuid(),
                SessionId = "cs_test_123",
                CheckoutUrl = "https://checkout.stripe.com/pay/cs_test_123"
            });

        var result = await _service.EnrollStudentAsync(courseId, new CreateCourseEnrollmentDto
        {
            StudentId = studentId,
            PaymentMethod = PaymentMethod.CreditCard,
            SuccessUrl = "https://app.example.com/org/demo/courses?checkout=success",
            CancelUrl = "https://app.example.com/org/demo/courses?checkout=cancel"
        }, keycloakUserId);

        Assert.Equal(studentId.ToString(), result.StudentId);
        Assert.Equal(courseId.ToString(), result.CourseId);
        Assert.Equal(nameof(EnrollmentType.Enrolled), result.EnrollmentType);
        Assert.Equal(45m, result.FeeAmount);
        Assert.Equal(0m, result.AmountPaid);
        Assert.Equal(nameof(PaymentStatus.Pending), result.PaymentStatus);
        Assert.False(string.IsNullOrWhiteSpace(result.PaymentId));
        Assert.True(result.RequiresCheckout);
        Assert.Equal("cs_test_123", result.CheckoutSessionId);
        Assert.Equal("https://checkout.stripe.com/pay/cs_test_123", result.CheckoutUrl);

        _tenantStripePaymentService.Verify(s => s.CreateCheckoutSessionAsync(It.Is<CreateTenantStripeCheckoutSessionDto>(dto =>
            dto.AccountHolderId == accountHolderId &&
            dto.EnrollmentId.HasValue &&
            dto.Amount == 45m &&
            dto.PaymentType == PaymentType.CourseFee &&
            dto.PaymentId == null), It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepository.Verify(r => r.CreateAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task EnrollStudentAsync_Should_Resume_Checkout_For_Pending_Paid_Enrollment()
    {
        var accountHolderId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var keycloakUserId = "keycloak-parent-resume";

        var accountHolder = new AccountHolder
        {
            Id = accountHolderId,
            KeycloakUserId = keycloakUserId,
            FirstName = "Pat",
            LastName = "Parent",
            EmailAddress = "pat.parent@example.com",
            Students = new List<Student>
            {
                new() { Id = studentId, AccountHolderId = accountHolderId, FirstName = "Sam", LastName = "Student" }
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

        var pendingEnrollment = new Enrollment
        {
            Id = enrollmentId,
            StudentId = studentId,
            CourseId = courseId,
            SemesterId = semesterId,
            EnrollmentType = EnrollmentType.Enrolled,
            FeeAmount = 45m,
            AmountPaid = 0m,
            PaymentStatus = PaymentStatus.Pending,
            Payments = new List<Payment>
            {
                new()
                {
                    Id = paymentId,
                    AccountHolderId = accountHolderId,
                    EnrollmentId = enrollmentId,
                    Amount = 45m,
                    PaymentType = PaymentType.CourseFee,
                    Notes = "Pending Stripe Checkout session"
                }
            }
        };

        _accountHolderRepository.Setup(r => r.GetByKeycloakUserIdAsync(keycloakUserId)).ReturnsAsync(accountHolder);
        _courseRepository.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(course);
        _enrollmentRepository.Setup(r => r.GetByStudentIdAsync(studentId, semesterId)).ReturnsAsync(new[] { pendingEnrollment });
        _enrollmentRepository.Setup(r => r.GetByIdAsync(enrollmentId)).ReturnsAsync(pendingEnrollment);

        _tenantStripePaymentService
            .Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<CreateTenantStripeCheckoutSessionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantStripeCheckoutSessionDto
            {
                PaymentId = paymentId,
                SessionId = "cs_test_resume",
                CheckoutUrl = "https://checkout.stripe.com/pay/cs_test_resume"
            });

        var result = await _service.EnrollStudentAsync(courseId, new CreateCourseEnrollmentDto
        {
            StudentId = studentId,
            SuccessUrl = "https://app.example.com/org/demo/courses?checkout=success",
            CancelUrl = "https://app.example.com/org/demo/courses?checkout=cancel"
        }, keycloakUserId);

        Assert.True(result.RequiresCheckout);
        Assert.Equal(paymentId.ToString(), result.PaymentId);
        Assert.Equal("cs_test_resume", result.CheckoutSessionId);
        _enrollmentRepository.Verify(r => r.CreateAsync(It.IsAny<Enrollment>()), Times.Never);
        _tenantStripePaymentService.Verify(s => s.CreateCheckoutSessionAsync(It.Is<CreateTenantStripeCheckoutSessionDto>(dto => dto.PaymentId == paymentId), It.IsAny<CancellationToken>()), Times.Once);
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

        _enrollmentRepository
            .Setup(r => r.GetByStudentIdAsync(It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Array.Empty<Enrollment>());

        var act = () => _service.EnrollStudentAsync(Guid.NewGuid(), new CreateCourseEnrollmentDto
        {
            StudentId = Guid.NewGuid()
        }, accountHolder.KeycloakUserId);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
        Assert.Equal("Student does not belong to the current account.", exception.Message);

        _enrollmentRepository.Verify(r => r.CreateAsync(It.IsAny<Enrollment>()), Times.Never);
        _paymentRepository.Verify(r => r.CreateAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task EnrollStudentAsync_Should_Block_ReEnrollment_When_Already_Enrolled()
    {
        var keycloakUserId = "keycloak-parent-3";
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var accountHolder = new AccountHolder
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            FirstName = "Pat", LastName = "Parent",
            EmailAddress = "pat@example.com",
            Students = new List<Student> { new() { Id = studentId, FirstName = "Sam", LastName = "Student" } }
        };
        _accountHolderRepository.Setup(r => r.GetByKeycloakUserIdAsync(keycloakUserId)).ReturnsAsync(accountHolder);
        _courseRepository.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(new Course
        {
            Id = courseId, SemesterId = Guid.NewGuid(), Name = "Art", MaxCapacity = 12, Enrollments = new List<Enrollment>()
        });
        _enrollmentRepository.Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<Guid?>())).ReturnsAsync(new[]
        {
            new Enrollment { StudentId = studentId, CourseId = courseId, EnrollmentType = EnrollmentType.Enrolled, PaymentStatus = PaymentStatus.Paid, AmountPaid = 10m }
        });

        var act = () => _service.EnrollStudentAsync(courseId, new CreateCourseEnrollmentDto { StudentId = studentId }, keycloakUserId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Contains("already signed up", exception.Message);
        _enrollmentRepository.Verify(r => r.CreateAsync(It.IsAny<Enrollment>()), Times.Never);
    }

    [Fact]
    public async Task EnrollStudentAsync_Should_Block_ReEnrollment_When_On_Waitlist()
    {
        var keycloakUserId = "keycloak-parent-4";
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var accountHolder = new AccountHolder
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            FirstName = "Pat", LastName = "Parent",
            EmailAddress = "pat@example.com",
            Students = new List<Student> { new() { Id = studentId, FirstName = "Sam", LastName = "Student" } }
        };
        _accountHolderRepository.Setup(r => r.GetByKeycloakUserIdAsync(keycloakUserId)).ReturnsAsync(accountHolder);
        _courseRepository.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(new Course
        {
            Id = courseId, SemesterId = Guid.NewGuid(), Name = "Art", MaxCapacity = 12, Enrollments = new List<Enrollment>()
        });
        _enrollmentRepository.Setup(r => r.GetByStudentIdAsync(studentId, It.IsAny<Guid?>())).ReturnsAsync(new[]
        {
            new Enrollment { StudentId = studentId, CourseId = courseId, EnrollmentType = EnrollmentType.Waitlisted, PaymentStatus = PaymentStatus.Paid, AmountPaid = 0m }
        });

        var act = () => _service.EnrollStudentAsync(courseId, new CreateCourseEnrollmentDto { StudentId = studentId }, keycloakUserId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Contains("already signed up", exception.Message);
        _enrollmentRepository.Verify(r => r.CreateAsync(It.IsAny<Enrollment>()), Times.Never);
    }

    [Fact]
    public async Task EnrollStudentAsync_Should_Allow_ReEnrollment_After_Withdrawal()
    {
        var keycloakUserId = "keycloak-parent-5";
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();
        var accountHolder = new AccountHolder
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            FirstName = "Pat", LastName = "Parent",
            EmailAddress = "pat@example.com",
            Students = new List<Student> { new() { Id = studentId, FirstName = "Sam", LastName = "Student" } }
        };
        _accountHolderRepository.Setup(r => r.GetByKeycloakUserIdAsync(keycloakUserId)).ReturnsAsync(accountHolder);
        _courseRepository.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(new Course
        {
            Id = courseId, SemesterId = semesterId, Name = "Art", MaxCapacity = 12, Fee = 0m, Enrollments = new List<Enrollment>()
        });
        _enrollmentRepository.Setup(r => r.GetByStudentIdAsync(studentId, semesterId)).ReturnsAsync(Array.Empty<Enrollment>());
        _enrollmentRepository.Setup(r => r.CreateAsync(It.IsAny<Enrollment>())).ReturnsAsync((Enrollment e) => e);
        _enrollmentRepository.Setup(r => r.UpdateAsync(It.IsAny<Enrollment>())).ReturnsAsync((Enrollment e) => e);
        _paymentRepository.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);

        var result = await _service.EnrollStudentAsync(courseId, new CreateCourseEnrollmentDto { StudentId = studentId }, keycloakUserId);

        Assert.Equal(studentId.ToString(), result.StudentId);
        Assert.Equal(nameof(EnrollmentType.Enrolled), result.EnrollmentType);
        _enrollmentRepository.Verify(r => r.CreateAsync(It.IsAny<Enrollment>()), Times.Once);
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

        Assert.Equal(accountHolderId, result.AccountHolderId);
        Assert.Equal(accountHolder.FirstName, result.FirstName);
        Assert.Equal(accountHolder.LastName, result.LastName);
        Assert.Equal(accountHolder.EmailAddress, result.Email);

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

        Assert.Equal(accountHolderId, result.AccountHolderId);

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
