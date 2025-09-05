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
