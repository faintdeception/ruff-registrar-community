using StudentRegistrar.Models;
using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface IStudentService
{
    Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
    Task<StudentDto?> GetStudentByIdAsync(Guid id);
    Task<StudentDto> CreateStudentAsync(CreateStudentDto createStudentDto);
    Task<StudentDto?> UpdateStudentAsync(Guid id, UpdateStudentDto updateStudentDto);
    Task<bool> DeleteStudentAsync(Guid id);
    Task<IEnumerable<EnrollmentDto>> GetStudentEnrollmentsAsync(Guid studentId);
    Task<IEnumerable<StudentDto>> GetStudentsByAccountHolderAsync(Guid accountHolderId);
}

public interface ICourseService
{
    Task<IEnumerable<CourseDto>> GetAllCoursesAsync();
    Task<CourseDto?> GetCourseByIdAsync(int id);
    Task<CourseDto> CreateCourseAsync(CreateCourseDto createCourseDto);
    Task<CourseDto?> UpdateCourseAsync(int id, UpdateCourseDto updateCourseDto);
    Task<bool> DeleteCourseAsync(int id);
    Task<IEnumerable<EnrollmentDto>> GetCourseEnrollmentsAsync(int courseId);
    Task<IEnumerable<GradeRecordDto>> GetCourseGradesAsync(int courseId);
}

public interface IEnrollmentService
{
    Task<IEnumerable<EnrollmentDto>> GetAllEnrollmentsAsync();
    Task<EnrollmentDto?> GetEnrollmentByIdAsync(Guid id);
    Task<EnrollmentDto> CreateEnrollmentAsync(CreateEnrollmentDto createEnrollmentDto);
    Task<bool> DeleteEnrollmentAsync(Guid id);
    Task<EnrollmentDto?> UpdateEnrollmentStatusAsync(Guid id, string status);
    Task<IEnumerable<EnrollmentDto>> GetEnrollmentsByStudentAsync(Guid studentId);
    Task<IEnumerable<EnrollmentDto>> GetEnrollmentsByCourseAsync(Guid courseId);
    Task<IEnumerable<EnrollmentDto>> GetEnrollmentsBySemesterAsync(Guid semesterId);
}

public interface IGradeService
{
    Task<IEnumerable<GradeRecordDto>> GetAllGradesAsync();
    Task<GradeRecordDto?> GetGradeByIdAsync(int id);
    Task<IEnumerable<GradeRecordDto>> GetGradesByStudentAsync(Guid studentId);
    Task<IEnumerable<GradeRecordDto>> GetGradesByCourseAsync(Guid courseId);
    Task<GradeRecordDto> CreateGradeAsync(CreateGradeRecordDto createGradeDto);
    Task<GradeRecordDto?> UpdateGradeAsync(int id, CreateGradeRecordDto updateGradeDto);
    Task<bool> DeleteGradeAsync(int id);
}

public interface IKeycloakService
{
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);
    Task UpdateUserRoleAsync(string keycloakId, UserRole role);
    Task DeactivateUserAsync(string keycloakId);
    Task<bool> UserExistsAsync(string email);
}

public interface ICourseInstructorService
{
    Task<IEnumerable<CourseInstructorDto>> GetAllCourseInstructorsAsync();
    Task<CourseInstructorDto?> GetCourseInstructorByIdAsync(Guid id);
    Task<IEnumerable<CourseInstructorDto>> GetCourseInstructorsByCourseIdAsync(Guid courseId);
    Task<CourseInstructorDto> CreateCourseInstructorAsync(CreateCourseInstructorDto createDto);
    Task<CourseInstructorDto?> UpdateCourseInstructorAsync(Guid id, UpdateCourseInstructorDto updateDto);
    Task<bool> DeleteCourseInstructorAsync(Guid id);
}

// Independent Educator Service (replaces CourseInstructor system)
public interface IEducatorService
{
    Task<IEnumerable<EducatorDto>> GetAllEducatorsAsync();
    Task<EducatorDto?> GetEducatorByIdAsync(Guid id);
    Task<IEnumerable<EducatorDto>> GetEducatorsByCourseIdAsync(Guid courseId);
    Task<IEnumerable<EducatorDto>> GetUnassignedEducatorsAsync();
    Task<EducatorDto> CreateEducatorAsync(CreateEducatorDto createDto);
    Task<EducatorDto?> UpdateEducatorAsync(Guid id, UpdateEducatorDto updateDto);
    Task<bool> DeleteEducatorAsync(Guid id);
    Task<bool> DeactivateEducatorAsync(Guid id);
    Task<bool> ActivateEducatorAsync(Guid id);
}

// New Course System Services
public interface ISemesterService
{
    Task<IEnumerable<SemesterDto>> GetAllSemestersAsync();
    Task<SemesterDto?> GetSemesterByIdAsync(Guid id);
    Task<SemesterDto?> GetActiveSemesterAsync();
    Task<SemesterDto> CreateSemesterAsync(CreateSemesterDto createDto);
    Task<SemesterDto?> UpdateSemesterAsync(Guid id, UpdateSemesterDto updateDto);
    Task<bool> DeleteSemesterAsync(Guid id);
}

public interface ICourseServiceV2
{
    Task<IEnumerable<CourseDto>> GetAllCoursesAsync();
    Task<IEnumerable<CourseDto>> GetCoursesBySemesterAsync(Guid semesterId);
    Task<CourseDto?> GetCourseByIdAsync(Guid id);
    Task<CourseDto> CreateCourseAsync(CreateCourseDto createDto);
    Task<CourseDto?> UpdateCourseAsync(Guid id, UpdateCourseDto updateDto);
    Task<bool> DeleteCourseAsync(Guid id);
    
    // Instructor management methods
    Task<IEnumerable<CourseInstructorDto>> GetCourseInstructorsAsync(Guid courseId);
    Task<CourseInstructorDto> AddInstructorAsync(CreateCourseInstructorDto createDto);
    Task<CourseInstructorDto?> UpdateInstructorAsync(Guid instructorId, UpdateCourseInstructorDto updateDto);
    Task<bool> RemoveInstructorAsync(Guid instructorId);
    Task<IEnumerable<AccountHolderDto>> GetAvailableMembersAsync();
}

public interface IAccountHolderService
{
    Task<IEnumerable<AccountHolderDto>> GetAllAccountHoldersAsync();
    Task<AccountHolderDto?> GetAccountHolderByUserIdAsync(string userId);
    Task<AccountHolderDto?> GetAccountHolderByIdAsync(Guid id);
    Task<AccountHolderDto> CreateAccountHolderAsync(CreateAccountHolderDto createDto);
    Task<AccountHolderDto> CreateAccountHolderAsync(CreateAccountHolderDto createDto, string? keycloakUserId);
    Task<AccountHolderDto?> UpdateAccountHolderAsync(Guid id, UpdateAccountHolderDto updateDto);
    Task<StudentDto> AddStudentToAccountAsync(Guid accountHolderId, CreateStudentForAccountDto createDto);
    Task<bool> RemoveStudentFromAccountAsync(Guid accountHolderId, Guid studentId);
}

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
    Task<PaymentDto?> GetPaymentByIdAsync(Guid id);
    Task<IEnumerable<PaymentDto>> GetPaymentsByAccountHolderAsync(Guid accountHolderId);
    Task<IEnumerable<PaymentDto>> GetPaymentsByEnrollmentAsync(Guid enrollmentId);
    Task<IEnumerable<PaymentDto>> GetPaymentsByTypeAsync(PaymentType paymentType);
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createDto);
    Task<PaymentDto?> UpdatePaymentAsync(Guid id, UpdatePaymentDto updateDto);
    Task<bool> DeletePaymentAsync(Guid id);
    Task<decimal> GetTotalPaidByAccountHolderAsync(Guid accountHolderId, PaymentType? type = null);
    Task<decimal> GetTotalPaidByEnrollmentAsync(Guid enrollmentId);
    Task<IEnumerable<PaymentDto>> GetPaymentHistoryAsync(Guid accountHolderId, DateTime? fromDate = null, DateTime? toDate = null);
}

public interface IRoomService
{
    Task<IEnumerable<RoomDto>> GetAllRoomsAsync();
    Task<RoomDto?> GetRoomByIdAsync(Guid id);
    Task<IEnumerable<RoomDto>> GetRoomsByTypeAsync(RoomType roomType);
    Task<RoomDto> CreateRoomAsync(CreateRoomDto createDto);
    Task<RoomDto?> UpdateRoomAsync(Guid id, UpdateRoomDto updateDto);
    Task<bool> DeleteRoomAsync(Guid id);
    Task<bool> IsRoomInUseAsync(Guid roomId);
}

public interface IPasswordService
{
    string GenerateSecurePassword(int length = 14);
    bool ValidatePasswordComplexity(string password);
    PasswordStrength AssessPasswordStrength(string password);
}

public enum PasswordStrength
{
    VeryWeak,
    Weak,
    Fair,
    Good,
    Strong,
    VeryStrong
}
