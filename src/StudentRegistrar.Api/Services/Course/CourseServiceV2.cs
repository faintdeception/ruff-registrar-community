using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class CourseServiceV2 : ICourseServiceV2
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseInstructorRepository _courseInstructorRepository;
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEducatorRepository _educatorRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IKeycloakService _keycloakService;
    private readonly IMapper _mapper;
    private readonly StudentRegistrarDbContext? _dbContext;

    public CourseServiceV2(
        ICourseRepository courseRepository, 
        ICourseInstructorRepository courseInstructorRepository,
        IAccountHolderRepository accountHolderRepository,
        IEnrollmentRepository enrollmentRepository,
        IPaymentRepository paymentRepository,
        IEducatorRepository educatorRepository,
        IRoomRepository roomRepository,
        IKeycloakService keycloakService,
        IMapper mapper,
        StudentRegistrarDbContext? dbContext = null)
    {
        _courseRepository = courseRepository;
        _courseInstructorRepository = courseInstructorRepository;
        _accountHolderRepository = accountHolderRepository;
        _enrollmentRepository = enrollmentRepository;
        _paymentRepository = paymentRepository;
        _educatorRepository = educatorRepository;
        _roomRepository = roomRepository;
        _keycloakService = keycloakService;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync()
    {
        var courses = await _courseRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<CourseDto>>(courses);
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesBySemesterAsync(Guid semesterId)
    {
        var courses = await _courseRepository.GetBySemesterAsync(semesterId);
        return _mapper.Map<IEnumerable<CourseDto>>(courses);
    }

    public async Task<CourseDto?> GetCourseByIdAsync(Guid id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        return course != null ? _mapper.Map<CourseDto>(course) : null;
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseDto createDto)
    {
        var course = _mapper.Map<Course>(createDto);
        var createdCourse = await _courseRepository.CreateAsync(course);
        return _mapper.Map<CourseDto>(createdCourse);
    }

    public async Task<CourseDto?> UpdateCourseAsync(Guid id, UpdateCourseDto updateDto)
    {
        var existingCourse = await _courseRepository.GetByIdAsync(id);
        if (existingCourse == null)
            return null;

        _mapper.Map(updateDto, existingCourse);
        var updatedCourse = await _courseRepository.UpdateAsync(existingCourse);
        return _mapper.Map<CourseDto>(updatedCourse);
    }

    public async Task<bool> DeleteCourseAsync(Guid id)
    {
        return await _courseRepository.DeleteAsync(id);
    }

    public async Task<CourseEnrollmentResultDto> EnrollStudentAsync(Guid courseId, CreateCourseEnrollmentDto createDto, string keycloakUserId)
    {
        if (string.IsNullOrWhiteSpace(keycloakUserId))
        {
            throw new UnauthorizedAccessException("User ID not found in token.");
        }

        var accountHolder = await _accountHolderRepository.GetByKeycloakUserIdAsync(keycloakUserId)
            ?? throw new InvalidOperationException("Account holder not found.");

        var student = accountHolder.Students.FirstOrDefault(s => s.Id == createDto.StudentId)
            ?? throw new UnauthorizedAccessException("Student does not belong to the current account.");

        var course = await _courseRepository.GetByIdAsync(courseId)
            ?? throw new KeyNotFoundException("Course not found.");

        if (await _enrollmentRepository.HasEnrollmentAsync(student.Id, course.Id))
        {
            throw new InvalidOperationException("This student is already signed up for this course.");
        }

        var isWaitlisted = course.CurrentEnrollment >= course.MaxCapacity;
        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = course.Id,
            SemesterId = course.SemesterId,
            EnrollmentType = isWaitlisted ? EnrollmentType.Waitlisted : EnrollmentType.Enrolled,
            EnrollmentDate = DateTime.UtcNow,
            FeeAmount = isWaitlisted ? 0 : course.Fee,
            PaymentStatus = isWaitlisted || course.Fee <= 0 ? PaymentStatus.Paid : PaymentStatus.Pending,
            WaitlistPosition = isWaitlisted ? await _enrollmentRepository.GetNextWaitlistPositionAsync(course.Id) : null
        };

        if (isWaitlisted)
        {
            enrollment.Notes = "Added to waitlist because the course is full.";
        }

        async Task<(Enrollment CreatedEnrollment, Payment? CreatedPayment)> CreateEnrollmentAndPaymentAsync()
        {
            var createdEnrollment = await _enrollmentRepository.CreateAsync(enrollment);
            Payment? createdPayment = null;

            if (!isWaitlisted && course.Fee > 0)
            {
                var paymentMethod = createDto.PaymentMethod ?? PaymentMethod.CreditCard;
                createdPayment = await _paymentRepository.CreateAsync(new Payment
                {
                    AccountHolderId = accountHolder.Id,
                    EnrollmentId = createdEnrollment.Id,
                    Amount = course.Fee,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = paymentMethod,
                    PaymentType = PaymentType.CourseFee,
                    TransactionId = $"course-signup-{createdEnrollment.Id:N}",
                    Notes = $"Course signup payment for {course.Name}."
                });

                createdEnrollment.AmountPaid = course.Fee;
                createdEnrollment.PaymentStatus = PaymentStatus.Paid;
                createdEnrollment = await _enrollmentRepository.UpdateAsync(createdEnrollment);
            }

            return (createdEnrollment, createdPayment);
        }

        var shouldCreateTransaction = _dbContext?.Database.CurrentTransaction == null;
        var (createdEnrollment, createdPayment) = _dbContext != null && shouldCreateTransaction
            ? await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                var result = await CreateEnrollmentAndPaymentAsync();
                await transaction.CommitAsync();
                return result;
            })
            : await CreateEnrollmentAndPaymentAsync();

        if (createdEnrollment == null)
        {
            throw new InvalidOperationException("Course enrollment could not be created.");
        }

        return new CourseEnrollmentResultDto
        {
            EnrollmentId = createdEnrollment.Id.ToString(),
            StudentId = student.Id.ToString(),
            StudentName = student.FullName,
            CourseId = course.Id.ToString(),
            CourseName = course.Name,
            EnrollmentType = createdEnrollment.EnrollmentType.ToString(),
            FeeAmount = createdEnrollment.FeeAmount,
            AmountPaid = createdEnrollment.AmountPaid,
            PaymentStatus = createdEnrollment.PaymentStatus.ToString(),
            PaymentId = createdPayment?.Id.ToString(),
            Message = createdEnrollment.EnrollmentType == EnrollmentType.Waitlisted
                ? "The course is full, so the student was added to the waitlist."
                : course.Fee > 0
                    ? "Student signed up and payment was recorded."
                    : "Student signed up successfully."
        };
    }

    // Instructor management methods
    public async Task<IEnumerable<CourseInstructorDto>> GetCourseInstructorsAsync(Guid courseId)
    {
        var instructors = await _courseInstructorRepository.GetByCourseIdAsync(courseId);
        return _mapper.Map<IEnumerable<CourseInstructorDto>>(instructors);
    }

    public async Task<CourseInstructorDto> AddInstructorAsync(CreateCourseInstructorDto createDto)
    {
        var instructor = _mapper.Map<CourseInstructor>(createDto);
        
        // If AccountHolderId is provided, populate name and email from the account holder
        if (createDto.AccountHolderId.HasValue)
        {
            var accountHolder = await _accountHolderRepository.GetByIdAsync(createDto.AccountHolderId.Value);
            if (accountHolder != null)
            {
                instructor.FirstName = accountHolder.FirstName;
                instructor.LastName = accountHolder.LastName;
                instructor.Email = accountHolder.EmailAddress;

                var keycloakUserId = await _keycloakService.GetUserIdByEmailAsync(accountHolder.EmailAddress);
                if (string.IsNullOrWhiteSpace(keycloakUserId) && !string.IsNullOrWhiteSpace(accountHolder.KeycloakUserId))
                {
                    keycloakUserId = accountHolder.KeycloakUserId;
                }

                if (string.IsNullOrWhiteSpace(keycloakUserId))
                {
                    throw new InvalidOperationException("Account holder does not have a Keycloak user.");
                }

                await _keycloakService.UpdateUserRoleAsync(keycloakUserId, UserRole.Educator);
                await UpsertEducatorForAccountHolderAsync(accountHolder, keycloakUserId);
            }
        }

        var createdInstructor = await _courseInstructorRepository.CreateAsync(instructor);
        return _mapper.Map<CourseInstructorDto>(createdInstructor);
    }

    private async Task UpsertEducatorForAccountHolderAsync(AccountHolder accountHolder, string keycloakUserId)
    {
        var existingEducator = await _educatorRepository.GetByAccountHolderIdAsync(accountHolder.Id);
        if (existingEducator != null)
        {
            existingEducator.FirstName = accountHolder.FirstName;
            existingEducator.LastName = accountHolder.LastName;
            existingEducator.Email = accountHolder.EmailAddress;
            existingEducator.Phone = accountHolder.MobilePhone ?? accountHolder.HomePhone;
            existingEducator.KeycloakUserId = keycloakUserId;
            existingEducator.IsActive = true;
            existingEducator.UpdatedAt = DateTime.UtcNow;

            await _educatorRepository.UpdateAsync(existingEducator);
            return;
        }

        await _educatorRepository.CreateAsync(new Educator
        {
            AccountHolderId = accountHolder.Id,
            FirstName = accountHolder.FirstName,
            LastName = accountHolder.LastName,
            Email = accountHolder.EmailAddress,
            Phone = accountHolder.MobilePhone ?? accountHolder.HomePhone,
            KeycloakUserId = keycloakUserId,
            IsActive = true
        });
    }

    public async Task<CourseInstructorDto?> UpdateInstructorAsync(Guid instructorId, UpdateCourseInstructorDto updateDto)
    {
        var existingInstructor = await _courseInstructorRepository.GetByIdAsync(instructorId);
        if (existingInstructor == null)
            return null;

        _mapper.Map(updateDto, existingInstructor);
        var updatedInstructor = await _courseInstructorRepository.UpdateAsync(existingInstructor);
        return _mapper.Map<CourseInstructorDto>(updatedInstructor);
    }

    public async Task<bool> RemoveInstructorAsync(Guid instructorId)
    {
        return await _courseInstructorRepository.DeleteAsync(instructorId);
    }

    public async Task<IEnumerable<AccountHolderDto>> GetAvailableMembersAsync()
    {
        var accountHolders = await _accountHolderRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<AccountHolderDto>>(accountHolders);
    }
}
