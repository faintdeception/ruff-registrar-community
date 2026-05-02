using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface IGradeService
{
    Task<IEnumerable<GradeRecordDto>> GetAllGradesAsync();
    Task<GradeRecordDto?> GetGradeByIdAsync(Guid id);
    Task<IEnumerable<GradeRecordDto>> GetGradesByStudentAsync(Guid studentId);
    Task<IEnumerable<GradeRecordDto>> GetGradesByCourseAsync(Guid courseId);
    Task<GradeRecordDto> CreateGradeAsync(CreateGradeRecordDto createGradeDto);
    Task<GradeRecordDto?> UpdateGradeAsync(Guid id, CreateGradeRecordDto updateGradeDto);
    Task<bool> DeleteGradeAsync(Guid id);
}
