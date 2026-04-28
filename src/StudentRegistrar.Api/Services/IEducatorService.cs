using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

// Educator profile and authorization service. Course assignments are managed through CourseInstructor.
public interface IEducatorService
{
    Task<IEnumerable<EducatorDto>> GetAllEducatorsAsync();
    Task<EducatorDto?> GetEducatorByIdAsync(Guid id);
    Task<EducatorDto> CreateEducatorAsync(CreateEducatorDto createDto);
    Task<InviteEducatorResponse> InviteEducatorAsync(InviteEducatorDto inviteDto);
    Task<EducatorDto?> UpdateEducatorAsync(Guid id, UpdateEducatorDto updateDto);
    Task<bool> DeleteEducatorAsync(Guid id);
    Task<bool> DeactivateEducatorAsync(Guid id);
    Task<bool> ActivateEducatorAsync(Guid id);
}
