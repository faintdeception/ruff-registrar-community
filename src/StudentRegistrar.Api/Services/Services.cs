using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StudentRegistrar.Api.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMapper _mapper;

    public StudentService(
        IStudentRepository studentRepository,
        IAccountHolderRepository accountHolderRepository,
        IEnrollmentRepository enrollmentRepository,
        IMapper mapper)
    {
        _studentRepository = studentRepository;
        _accountHolderRepository = accountHolderRepository;
        _enrollmentRepository = enrollmentRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
    {
        var students = await _studentRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<StudentDto>>(students);
    }

    public async Task<StudentDto?> GetStudentByIdAsync(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);
        return student != null ? _mapper.Map<StudentDto>(student) : null;
    }

    public async Task<StudentDto> CreateStudentAsync(CreateStudentDto createStudentDto)
    {
        var student = _mapper.Map<Student>(createStudentDto);
        var createdStudent = await _studentRepository.CreateAsync(student);
        return _mapper.Map<StudentDto>(createdStudent);
    }

    public async Task<StudentDto?> UpdateStudentAsync(Guid id, UpdateStudentDto updateStudentDto)
    {
        var existingStudent = await _studentRepository.GetByIdAsync(id);
        if (existingStudent == null)
            return null;

        _mapper.Map(updateStudentDto, existingStudent);
        var updatedStudent = await _studentRepository.UpdateAsync(existingStudent);
        return _mapper.Map<StudentDto>(updatedStudent);
    }

    public async Task<bool> DeleteStudentAsync(Guid id)
    {
        return await _studentRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<EnrollmentDto>> GetStudentEnrollmentsAsync(Guid studentId)
    {
        var enrollments = await _enrollmentRepository.GetByStudentAsync(studentId);
        return _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments);
    }

    public async Task<IEnumerable<StudentDto>> GetStudentsByAccountHolderAsync(Guid accountHolderId)
    {
        var students = await _studentRepository.GetByAccountHolderAsync(accountHolderId);
        return _mapper.Map<IEnumerable<StudentDto>>(students);
    }
}

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        IMapper mapper)
    {
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<EnrollmentDto>> GetAllEnrollmentsAsync()
    {
        var enrollments = await _enrollmentRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments);
    }

    public async Task<EnrollmentDto?> GetEnrollmentByIdAsync(Guid id)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id);
        return enrollment != null ? _mapper.Map<EnrollmentDto>(enrollment) : null;
    }

    public async Task<EnrollmentDto> CreateEnrollmentAsync(CreateEnrollmentDto createEnrollmentDto)
    {
        var enrollment = _mapper.Map<Enrollment>(createEnrollmentDto);
        var createdEnrollment = await _enrollmentRepository.CreateAsync(enrollment);
        return _mapper.Map<EnrollmentDto>(createdEnrollment);
    }

    public async Task<bool> DeleteEnrollmentAsync(Guid id)
    {
        return await _enrollmentRepository.DeleteAsync(id);
    }

    public async Task<EnrollmentDto?> UpdateEnrollmentStatusAsync(Guid id, string status)
    {
        var existingEnrollment = await _enrollmentRepository.GetByIdAsync(id);
        if (existingEnrollment == null)
            return null;

        // Update the enrollment type based on status
        if (Enum.TryParse<EnrollmentType>(status, out var enrollmentType))
        {
            existingEnrollment.EnrollmentType = enrollmentType;
            var updatedEnrollment = await _enrollmentRepository.UpdateAsync(existingEnrollment);
            return _mapper.Map<EnrollmentDto>(updatedEnrollment);
        }

        return null;
    }

    public async Task<IEnumerable<EnrollmentDto>> GetEnrollmentsByStudentAsync(Guid studentId)
    {
        var enrollments = await _enrollmentRepository.GetByStudentAsync(studentId);
        return _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments);
    }

    public async Task<IEnumerable<EnrollmentDto>> GetEnrollmentsByCourseAsync(Guid courseId)
    {
        var enrollments = await _enrollmentRepository.GetByCourseAsync(courseId);
        return _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments);
    }

    public async Task<IEnumerable<EnrollmentDto>> GetEnrollmentsBySemesterAsync(Guid semesterId)
    {
        var enrollments = await _enrollmentRepository.GetBySemesterAsync(semesterId);
        return _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments);
    }
}

public class CourseServiceV2 : ICourseServiceV2
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseInstructorRepository _courseInstructorRepository;
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IMapper _mapper;

    public CourseServiceV2(
        ICourseRepository courseRepository, 
        ICourseInstructorRepository courseInstructorRepository,
        IAccountHolderRepository accountHolderRepository,
        IRoomRepository roomRepository,
        IMapper mapper)
    {
        _courseRepository = courseRepository;
        _courseInstructorRepository = courseInstructorRepository;
        _accountHolderRepository = accountHolderRepository;
        _roomRepository = roomRepository;
        _mapper = mapper;
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
            }
        }

        var createdInstructor = await _courseInstructorRepository.CreateAsync(instructor);
        return _mapper.Map<CourseInstructorDto>(createdInstructor);
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

public class AccountHolderService : IAccountHolderService
{
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IMapper _mapper;

    public AccountHolderService(
        IAccountHolderRepository accountHolderRepository,
        IStudentRepository studentRepository,
        IMapper mapper)
    {
        _accountHolderRepository = accountHolderRepository;
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AccountHolderDto>> GetAllAccountHoldersAsync()
    {
        var accountHolders = await _accountHolderRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<AccountHolderDto>>(accountHolders);
    }

    public async Task<AccountHolderDto?> GetAccountHolderByUserIdAsync(string userId)
    {
        var accountHolder = await _accountHolderRepository.GetByKeycloakUserIdAsync(userId);
        return accountHolder != null ? _mapper.Map<AccountHolderDto>(accountHolder) : null;
    }

    public async Task<AccountHolderDto?> GetAccountHolderByIdAsync(Guid id)
    {
        var accountHolder = await _accountHolderRepository.GetByIdAsync(id);
        return accountHolder != null ? _mapper.Map<AccountHolderDto>(accountHolder) : null;
    }

    public async Task<AccountHolderDto> CreateAccountHolderAsync(CreateAccountHolderDto createDto)
    {
        var accountHolder = _mapper.Map<AccountHolder>(createDto);
        var createdAccountHolder = await _accountHolderRepository.CreateAsync(accountHolder);
        return _mapper.Map<AccountHolderDto>(createdAccountHolder);
    }

    public async Task<AccountHolderDto> CreateAccountHolderAsync(CreateAccountHolderDto createDto, string? keycloakUserId)
    {
        var accountHolder = _mapper.Map<AccountHolder>(createDto);
        if (!string.IsNullOrEmpty(keycloakUserId))
        {
            accountHolder.KeycloakUserId = keycloakUserId;
        }
        var createdAccountHolder = await _accountHolderRepository.CreateAsync(accountHolder);
        return _mapper.Map<AccountHolderDto>(createdAccountHolder);
    }

    public async Task<AccountHolderDto?> UpdateAccountHolderAsync(Guid id, UpdateAccountHolderDto updateDto)
    {
        var existingAccountHolder = await _accountHolderRepository.GetByIdAsync(id);
        if (existingAccountHolder == null)
            return null;

        _mapper.Map(updateDto, existingAccountHolder);
        var updatedAccountHolder = await _accountHolderRepository.UpdateAsync(existingAccountHolder);
        return _mapper.Map<AccountHolderDto>(updatedAccountHolder);
    }

    public async Task<StudentDto> AddStudentToAccountAsync(Guid accountHolderId, CreateStudentForAccountDto createDto)
    {
        var student = _mapper.Map<Student>(createDto);
        student.AccountHolderId = accountHolderId;
        
        var createdStudent = await _studentRepository.CreateAsync(student);
        return _mapper.Map<StudentDto>(createdStudent);
    }

    public async Task<bool> RemoveStudentFromAccountAsync(Guid accountHolderId, Guid studentId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null || student.AccountHolderId != accountHolderId)
            return false;

        return await _studentRepository.DeleteAsync(studentId);
    }
}

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IMapper _mapper;

    public SemesterService(ISemesterRepository semesterRepository, IMapper mapper)
    {
        _semesterRepository = semesterRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SemesterDto>> GetAllSemestersAsync()
    {
        var semesters = await _semesterRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<SemesterDto>>(semesters);
    }

    public async Task<SemesterDto?> GetSemesterByIdAsync(Guid id)
    {
        var semester = await _semesterRepository.GetByIdAsync(id);
        return semester != null ? _mapper.Map<SemesterDto>(semester) : null;
    }

    public async Task<SemesterDto?> GetActiveSemesterAsync()
    {
        var semester = await _semesterRepository.GetActiveAsync();
        return semester != null ? _mapper.Map<SemesterDto>(semester) : null;
    }

    public async Task<SemesterDto> CreateSemesterAsync(CreateSemesterDto createDto)
    {
        var semester = _mapper.Map<Semester>(createDto);
        var createdSemester = await _semesterRepository.CreateAsync(semester);
        return _mapper.Map<SemesterDto>(createdSemester);
    }

    public async Task<SemesterDto?> UpdateSemesterAsync(Guid id, UpdateSemesterDto updateDto)
    {
        var existingSemester = await _semesterRepository.GetByIdAsync(id);
        if (existingSemester == null)
            return null;

        _mapper.Map(updateDto, existingSemester);
        var updatedSemester = await _semesterRepository.UpdateAsync(existingSemester);
        return _mapper.Map<SemesterDto>(updatedSemester);
    }

    public async Task<bool> DeleteSemesterAsync(Guid id)
    {
        return await _semesterRepository.DeleteAsync(id);
    }
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;

    public PaymentService(IPaymentRepository paymentRepository, IMapper mapper)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
    {
        var payments = await _paymentRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByAccountHolderAsync(Guid accountHolderId)
    {
        var payments = await _paymentRepository.GetByAccountHolderIdAsync(accountHolderId);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByEnrollmentAsync(Guid enrollmentId)
    {
        var payments = await _paymentRepository.GetByEnrollmentIdAsync(enrollmentId);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByTypeAsync(PaymentType paymentType)
    {
        var payments = await _paymentRepository.GetByTypeAsync(paymentType);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createDto)
    {
        var payment = _mapper.Map<Payment>(createDto);
        var createdPayment = await _paymentRepository.CreateAsync(payment);
        return _mapper.Map<PaymentDto>(createdPayment);
    }

    public async Task<PaymentDto?> UpdatePaymentAsync(Guid id, UpdatePaymentDto updateDto)
    {
        var existingPayment = await _paymentRepository.GetByIdAsync(id);
        if (existingPayment == null)
            return null;

        _mapper.Map(updateDto, existingPayment);
        var updatedPayment = await _paymentRepository.UpdateAsync(existingPayment);
        return _mapper.Map<PaymentDto>(updatedPayment);
    }

    public async Task<bool> DeletePaymentAsync(Guid id)
    {
        return await _paymentRepository.DeleteAsync(id);
    }

    public async Task<decimal> GetTotalPaidByAccountHolderAsync(Guid accountHolderId, PaymentType? type = null)
    {
        return await _paymentRepository.GetTotalPaidByAccountHolderAsync(accountHolderId, type);
    }

    public async Task<decimal> GetTotalPaidByEnrollmentAsync(Guid enrollmentId)
    {
        return await _paymentRepository.GetTotalPaidByEnrollmentAsync(enrollmentId);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentHistoryAsync(Guid accountHolderId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var payments = await _paymentRepository.GetPaymentHistoryAsync(accountHolderId, fromDate, toDate);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }
}

public class CourseInstructorService : ICourseInstructorService
{
    private readonly ICourseInstructorRepository _courseInstructorRepository;
    private readonly IMapper _mapper;

    public CourseInstructorService(ICourseInstructorRepository courseInstructorRepository, IMapper mapper)
    {
        _courseInstructorRepository = courseInstructorRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CourseInstructorDto>> GetAllCourseInstructorsAsync()
    {
        var courseInstructors = await _courseInstructorRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<CourseInstructorDto>>(courseInstructors);
    }

    public async Task<CourseInstructorDto?> GetCourseInstructorByIdAsync(Guid id)
    {
        var courseInstructor = await _courseInstructorRepository.GetByIdAsync(id);
        return courseInstructor != null ? _mapper.Map<CourseInstructorDto>(courseInstructor) : null;
    }

    public async Task<IEnumerable<CourseInstructorDto>> GetCourseInstructorsByCourseIdAsync(Guid courseId)
    {
        var courseInstructors = await _courseInstructorRepository.GetByCourseIdAsync(courseId);
        return _mapper.Map<IEnumerable<CourseInstructorDto>>(courseInstructors);
    }

    public async Task<CourseInstructorDto> CreateCourseInstructorAsync(CreateCourseInstructorDto createDto)
    {
        var courseInstructor = _mapper.Map<CourseInstructor>(createDto);
        var createdCourseInstructor = await _courseInstructorRepository.CreateAsync(courseInstructor);
        return _mapper.Map<CourseInstructorDto>(createdCourseInstructor);
    }

    public async Task<CourseInstructorDto?> UpdateCourseInstructorAsync(Guid id, UpdateCourseInstructorDto updateDto)
    {
        var existingCourseInstructor = await _courseInstructorRepository.GetByIdAsync(id);
        if (existingCourseInstructor == null)
            return null;

        _mapper.Map(updateDto, existingCourseInstructor);
        var updatedCourseInstructor = await _courseInstructorRepository.UpdateAsync(existingCourseInstructor);
        return _mapper.Map<CourseInstructorDto>(updatedCourseInstructor);
    }

    public async Task<bool> DeleteCourseInstructorAsync(Guid id)
    {
        return await _courseInstructorRepository.DeleteAsync(id);
    }
}

public class GradeService : IGradeService
{
    private readonly StudentRegistrar.Data.Repositories.IGradeRepository _gradeRepository;
    private readonly IMapper _mapper;

    public GradeService(
        StudentRegistrar.Data.Repositories.IGradeRepository gradeRepository,
        IMapper mapper)
    {
        _gradeRepository = gradeRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<GradeRecordDto>> GetAllGradesAsync()
    {
        var grades = await _gradeRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<GradeRecordDto>>(grades);
    }

    public async Task<GradeRecordDto?> GetGradeByIdAsync(int id)
    {
        var grade = await _gradeRepository.GetByIdAsync(id);
        return grade == null ? null : _mapper.Map<GradeRecordDto>(grade);
    }

    public async Task<IEnumerable<GradeRecordDto>> GetGradesByStudentAsync(Guid studentId)
    {
        var grades = await _gradeRepository.GetByStudentIdAsync(studentId);
        return _mapper.Map<IEnumerable<GradeRecordDto>>(grades);
    }

    public async Task<IEnumerable<GradeRecordDto>> GetGradesByCourseAsync(Guid courseId)
    {
        var grades = await _gradeRepository.GetByCourseIdAsync(courseId);
        return _mapper.Map<IEnumerable<GradeRecordDto>>(grades);
    }

    public async Task<GradeRecordDto> CreateGradeAsync(CreateGradeRecordDto createGradeDto)
    {
        var grade = _mapper.Map<GradeRecord>(createGradeDto);
        var createdGrade = await _gradeRepository.CreateAsync(grade);
        return _mapper.Map<GradeRecordDto>(createdGrade);
    }

    public async Task<GradeRecordDto?> UpdateGradeAsync(int id, CreateGradeRecordDto updateGradeDto)
    {
        var existingGrade = await _gradeRepository.GetByIdAsync(id);
        if (existingGrade == null)
            return null;

        _mapper.Map(updateGradeDto, existingGrade);
        var updatedGrade = await _gradeRepository.UpdateAsync(existingGrade);
        return _mapper.Map<GradeRecordDto>(updatedGrade);
    }

    public async Task<bool> DeleteGradeAsync(int id)
    {
        return await _gradeRepository.DeleteAsync(id);
    }
}

public class EducatorService : IEducatorService
{
    private readonly StudentRegistrar.Data.Repositories.IEducatorRepository _educatorRepository;
    private readonly IMapper _mapper;

    public EducatorService(
        StudentRegistrar.Data.Repositories.IEducatorRepository educatorRepository,
        IMapper mapper)
    {
        _educatorRepository = educatorRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<EducatorDto>> GetAllEducatorsAsync()
    {
        var educators = await _educatorRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<EducatorDto>>(educators);
    }

    public async Task<EducatorDto?> GetEducatorByIdAsync(Guid id)
    {
        var educator = await _educatorRepository.GetByIdAsync(id);
        return educator == null ? null : _mapper.Map<EducatorDto>(educator);
    }

    public async Task<IEnumerable<EducatorDto>> GetEducatorsByCourseIdAsync(Guid courseId)
    {
        var educators = await _educatorRepository.GetByCourseIdAsync(courseId);
        return _mapper.Map<IEnumerable<EducatorDto>>(educators);
    }

    public async Task<IEnumerable<EducatorDto>> GetUnassignedEducatorsAsync()
    {
        var educators = await _educatorRepository.GetUnassignedAsync();
        return _mapper.Map<IEnumerable<EducatorDto>>(educators);
    }

    public async Task<EducatorDto> CreateEducatorAsync(CreateEducatorDto createDto)
    {
        var educator = _mapper.Map<Educator>(createDto);
        educator.IsActive = true; // Set as active by default
        var createdEducator = await _educatorRepository.CreateAsync(educator);
        return _mapper.Map<EducatorDto>(createdEducator);
    }

    public async Task<EducatorDto?> UpdateEducatorAsync(Guid id, UpdateEducatorDto updateDto)
    {
        var existingEducator = await _educatorRepository.GetByIdAsync(id);
        if (existingEducator == null)
            return null;

        _mapper.Map(updateDto, existingEducator);
        var updatedEducator = await _educatorRepository.UpdateAsync(existingEducator);
        return _mapper.Map<EducatorDto>(updatedEducator);
    }

    public async Task<bool> DeleteEducatorAsync(Guid id)
    {
        return await _educatorRepository.DeleteAsync(id);
    }

    public async Task<bool> DeactivateEducatorAsync(Guid id)
    {
        return await _educatorRepository.DeactivateAsync(id);
    }

    public async Task<bool> ActivateEducatorAsync(Guid id)
    {
        return await _educatorRepository.ActivateAsync(id);
    }
}

public class KeycloakService : IKeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakService> _logger;
    private readonly IPasswordService _passwordService;
    private readonly string _keycloakBaseUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public KeycloakService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakService> logger,
        IPasswordService passwordService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _passwordService = passwordService;
        
        // Load Keycloak configuration
        _keycloakBaseUrl = _configuration["Keycloak:BaseUrl"] ?? "http://localhost:8080";
        _realm = _configuration["Keycloak:Realm"] ?? "student-registrar";
        _clientId = _configuration["Keycloak:ClientId"] ?? "student-registrar";
        _clientSecret = _configuration["Keycloak:ClientSecret"] ?? throw new InvalidOperationException("Keycloak ClientSecret is required");
        
        _logger.LogInformation("Keycloak service initialized with base URL: {BaseUrl}, realm: {Realm}", _keycloakBaseUrl, _realm);
    }

    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            _logger.LogInformation("Creating user in Keycloak with email: {Email}", request.Email);
            
            // Get admin access token
            var adminToken = await GetAdminAccessTokenAsync();
            
            // Generate a secure temporary password
            var temporaryPassword = _passwordService.GenerateSecurePassword(14);
            var passwordStrength = _passwordService.AssessPasswordStrength(temporaryPassword);
            
            _logger.LogDebug("Generated temporary password with strength: {Strength}", passwordStrength);
            
            // Create user representation for Keycloak
            var keycloakUser = new
            {
                username = request.Email,
                email = request.Email,
                firstName = request.FirstName,
                lastName = request.LastName,
                enabled = true,
                emailVerified = false,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = temporaryPassword,
                        temporary = true
                    }
                },
                requiredActions = new[] { "UPDATE_PASSWORD", "VERIFY_EMAIL" }
            };
            
            // Make the API call to create user
            using var createRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms/{_realm}/users");
            createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            createRequest.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(keycloakUser),
                System.Text.Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.SendAsync(createRequest);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                // Extract user ID from Location header
                var locationHeader = response.Headers.Location?.ToString();
                var userId = locationHeader?.Split('/').LastOrDefault();
                
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("Failed to extract user ID from Keycloak response");
                }
                
                _logger.LogInformation("Successfully created user in Keycloak with ID: {UserId}", userId);
                
                return new CreateUserResponse
                {
                    UserId = userId,
                    Username = request.Email,
                    TemporaryPassword = temporaryPassword,
                    IsTemporary = true
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException($"User with email {request.Email} already exists in Keycloak");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to create user in Keycloak. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user in Keycloak for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task UpdateUserRoleAsync(string keycloakId, UserRole role)
    {
        try
        {
            _logger.LogInformation("Updating user role for Keycloak ID: {KeycloakId} to role: {Role}", keycloakId, role);
            
            // Get admin access token
            var adminToken = await GetAdminAccessTokenAsync();
            
            // Map UserRole to Keycloak role name
            var keycloakRoleName = role switch
            {
                UserRole.Administrator => "admin",
                UserRole.Educator => "educator",
                UserRole.Member => "student",
                _ => throw new ArgumentException($"Unsupported role: {role}")
            };
            
            // Get the role representation
            using var getRoleRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{_realm}/roles/{keycloakRoleName}");
            getRoleRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var getRoleResponse = await _httpClient.SendAsync(getRoleRequest);
            if (!getRoleResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Role '{keycloakRoleName}' not found in Keycloak realm");
            }
            
            var roleJson = await getRoleResponse.Content.ReadAsStringAsync();
            var roleRepresentation = System.Text.Json.JsonSerializer.Deserialize<object>(roleJson);
            
            // Assign role to user
            using var assignRoleRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm");
            assignRoleRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            assignRoleRequest.Content = new StringContent(
                $"[{roleJson}]",
                System.Text.Encoding.UTF8,
                "application/json");
            
            var assignResponse = await _httpClient.SendAsync(assignRoleRequest);
            if (assignResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated user role for Keycloak ID: {KeycloakId} to {Role}", keycloakId, role);
            }
            else
            {
                var errorContent = await assignResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to assign role to user. Status: {assignResponse.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user role for Keycloak ID: {KeycloakId}", keycloakId);
            throw;
        }
    }

    public async Task DeactivateUserAsync(string keycloakId)
    {
        try
        {
            _logger.LogInformation("Deactivating user for Keycloak ID: {KeycloakId}", keycloakId);
            
            // Get admin access token
            var adminToken = await GetAdminAccessTokenAsync();
            
            // Update user to set enabled = false
            var userUpdate = new { enabled = false };
            
            using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"{_keycloakBaseUrl}/admin/realms/{_realm}/users/{keycloakId}");
            updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            updateRequest.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(userUpdate),
                System.Text.Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.SendAsync(updateRequest);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deactivated user for Keycloak ID: {KeycloakId}", keycloakId);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to deactivate user. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate user for Keycloak ID: {KeycloakId}", keycloakId);
            throw;
        }
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        try
        {
            _logger.LogInformation("Checking if user exists with email: {Email}", email);
            
            // Get admin access token
            var adminToken = await GetAdminAccessTokenAsync();
            
            // Search for user by email
            using var searchRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{_realm}/users?email={Uri.EscapeDataString(email)}&exact=true");
            searchRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var response = await _httpClient.SendAsync(searchRequest);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                var users = jsonDoc.RootElement;
                
                bool userExists = users.GetArrayLength() > 0;
                _logger.LogInformation("User exists check for {Email}: {Exists}", email, userExists);
                return userExists;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to search for user. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user exists for email: {Email}", email);
            throw;
        }
    }

    private async Task<string> GetAdminAccessTokenAsync()
    {
        try
        {
            _logger.LogDebug("Obtaining admin access token from Keycloak");
            
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _clientId },
                { "client_secret", _clientSecret }
            };
            
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/realms/{_realm}/protocol/openid-connect/token");
            request.Content = new FormUrlEncodedContent(tokenRequest);
            
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new InvalidOperationException("Access token is null or empty in Keycloak response");
                }
                
                _logger.LogDebug("Successfully obtained admin access token");
                return accessToken;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to obtain access token. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain admin access token from Keycloak");
            throw;
        }
    }
}

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IMapper _mapper;

    public RoomService(IRoomRepository roomRepository, IMapper mapper)
    {
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync()
    {
        var rooms = await _roomRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<RoomDto>>(rooms);
    }

    public async Task<RoomDto?> GetRoomByIdAsync(Guid id)
    {
        var room = await _roomRepository.GetByIdAsync(id);
        return room == null ? null : _mapper.Map<RoomDto>(room);
    }

    public async Task<IEnumerable<RoomDto>> GetRoomsByTypeAsync(RoomType roomType)
    {
        var rooms = await _roomRepository.GetByTypeAsync(roomType);
        return _mapper.Map<IEnumerable<RoomDto>>(rooms);
    }

    public async Task<RoomDto> CreateRoomAsync(CreateRoomDto createDto)
    {
        // Check if room name already exists
        var existingRoom = await _roomRepository.GetByNameAsync(createDto.Name);
        if (existingRoom != null)
        {
            throw new InvalidOperationException($"A room with the name '{createDto.Name}' already exists.");
        }

        var room = _mapper.Map<Room>(createDto);
        var createdRoom = await _roomRepository.CreateAsync(room);
        return _mapper.Map<RoomDto>(createdRoom);
    }

    public async Task<RoomDto?> UpdateRoomAsync(Guid id, UpdateRoomDto updateDto)
    {
        var existingRoom = await _roomRepository.GetByIdAsync(id);
        if (existingRoom == null)
            return null;

        // Check if name is being changed and if it conflicts with another room
        if (existingRoom.Name != updateDto.Name)
        {
            var nameExists = await _roomRepository.NameExistsAsync(updateDto.Name, id);
            if (nameExists)
            {
                throw new InvalidOperationException($"A room with the name '{updateDto.Name}' already exists.");
            }
        }

        _mapper.Map(updateDto, existingRoom);
        var updatedRoom = await _roomRepository.UpdateAsync(existingRoom);
        return _mapper.Map<RoomDto>(updatedRoom);
    }

    public async Task<bool> DeleteRoomAsync(Guid id)
    {
        // Check if room is in use by any courses
        var isInUse = await _roomRepository.IsRoomInUseAsync(id);
        if (isInUse)
        {
            throw new InvalidOperationException("Cannot delete room because it is currently assigned to one or more courses.");
        }

        return await _roomRepository.DeleteAsync(id);
    }

    public async Task<bool> IsRoomInUseAsync(Guid roomId)
    {
        return await _roomRepository.IsRoomInUseAsync(roomId);
    }
}

public class PasswordService : IPasswordService
{
    private readonly ILogger<PasswordService> _logger;
    
    // Character sets for password generation (excluding ambiguous characters)
    private const string UppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Exclude I, O
    private const string LowercaseChars = "abcdefghjkmnpqrstuvwxyz"; // Exclude i, l, o
    private const string DigitChars = "23456789"; // Exclude 0, 1
    private const string SpecialChars = "!@#$%&*+=?";
    private const string AllChars = UppercaseChars + LowercaseChars + DigitChars + SpecialChars;

    public PasswordService(ILogger<PasswordService> logger)
    {
        _logger = logger;
    }

    public string GenerateSecurePassword(int length = 14)
    {
        if (length < 8)
        {
            throw new ArgumentException("Password length must be at least 8 characters", nameof(length));
        }

        try
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var password = new char[length];
            var categoryUsed = new bool[4]; // Track which character categories we've used

            // Ensure at least one character from each category
            password[0] = GetRandomChar(UppercaseChars, rng);
            categoryUsed[0] = true;
            password[1] = GetRandomChar(LowercaseChars, rng);
            categoryUsed[1] = true;
            password[2] = GetRandomChar(DigitChars, rng);
            categoryUsed[2] = true;
            password[3] = GetRandomChar(SpecialChars, rng);
            categoryUsed[3] = true;

            // Fill the rest with random characters from all categories
            for (int i = 4; i < length; i++)
            {
                password[i] = GetRandomChar(AllChars, rng);
            }

            // Shuffle the password to avoid predictable patterns
            ShuffleArray(password, rng);

            var result = new string(password);
            
            // Validate the generated password meets our complexity requirements
            if (!ValidatePasswordComplexity(result))
            {
                _logger.LogWarning("Generated password failed complexity validation, regenerating...");
                return GenerateSecurePassword(length); // Recursive retry
            }

            _logger.LogDebug("Generated secure password of length {Length}", length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure password");
            throw;
        }
    }

    public bool ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        var hasUpper = password.Any(c => UppercaseChars.Contains(c));
        var hasLower = password.Any(c => LowercaseChars.Contains(c));
        var hasDigit = password.Any(c => DigitChars.Contains(c));
        var hasSpecial = password.Any(c => SpecialChars.Contains(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    public PasswordStrength AssessPasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return PasswordStrength.VeryWeak;

        int score = 0;

        // Length scoring
        if (password.Length >= 8) score += 1;
        if (password.Length >= 12) score += 1;
        if (password.Length >= 16) score += 1;

        // Character diversity scoring
        if (password.Any(c => UppercaseChars.Contains(c))) score += 1;
        if (password.Any(c => LowercaseChars.Contains(c))) score += 1;
        if (password.Any(c => DigitChars.Contains(c))) score += 1;
        if (password.Any(c => SpecialChars.Contains(c))) score += 1;

        // Pattern analysis (basic)
        if (!HasRepeatingPatterns(password)) score += 1;
        if (!HasSequentialPatterns(password)) score += 1;

        return score switch
        {
            <= 2 => PasswordStrength.VeryWeak,
            3 => PasswordStrength.Weak,
            4 => PasswordStrength.Fair,
            5 => PasswordStrength.Good,
            6 => PasswordStrength.Strong,
            >= 7 => PasswordStrength.VeryStrong,
        };
    }

    private static char GetRandomChar(string chars, System.Security.Cryptography.RandomNumberGenerator rng)
    {
        var randomBytes = new byte[4];
        rng.GetBytes(randomBytes);
        var randomValue = BitConverter.ToUInt32(randomBytes, 0);
        return chars[(int)(randomValue % chars.Length)];
    }

    private static void ShuffleArray(char[] array, System.Security.Cryptography.RandomNumberGenerator rng)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            var randomValue = BitConverter.ToUInt32(randomBytes, 0);
            int j = (int)(randomValue % (i + 1));
            
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    private static bool HasRepeatingPatterns(string password)
    {
        // Check for 3+ character repetitions
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasSequentialPatterns(string password)
    {
        // Check for 3+ character sequences (abc, 123, etc.)
        for (int i = 0; i < password.Length - 2; i++)
        {
            if ((password[i + 1] == password[i] + 1) && (password[i + 2] == password[i + 1] + 1))
            {
                return true;
            }
        }
        return false;
    }
}

