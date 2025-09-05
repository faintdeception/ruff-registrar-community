using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

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
