using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

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
