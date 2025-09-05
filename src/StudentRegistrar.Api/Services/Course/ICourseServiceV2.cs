using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

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
