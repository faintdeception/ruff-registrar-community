using StudentRegistrar.Models;
using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;



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
