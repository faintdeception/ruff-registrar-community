using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface ISemesterService
{
    Task<IEnumerable<SemesterDto>> GetAllSemestersAsync();
    Task<SemesterDto?> GetSemesterByIdAsync(Guid id);
    Task<SemesterDto?> GetActiveSemesterAsync();
    Task<SemesterDto> CreateSemesterAsync(CreateSemesterDto createDto);
    Task<SemesterDto?> UpdateSemesterAsync(Guid id, UpdateSemesterDto updateDto);
    Task<bool> DeleteSemesterAsync(Guid id);
}
