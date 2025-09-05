using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface ICourseInstructorService
{
    Task<IEnumerable<CourseInstructorDto>> GetAllCourseInstructorsAsync();
    Task<CourseInstructorDto?> GetCourseInstructorByIdAsync(Guid id);
    Task<IEnumerable<CourseInstructorDto>> GetCourseInstructorsByCourseIdAsync(Guid courseId);
    Task<CourseInstructorDto> CreateCourseInstructorAsync(CreateCourseInstructorDto createDto);
    Task<CourseInstructorDto?> UpdateCourseInstructorAsync(Guid id, UpdateCourseInstructorDto updateDto);
    Task<bool> DeleteCourseInstructorAsync(Guid id);
}
