using StudentRegistrar.Models;

namespace StudentRegistrar.Data.Repositories;

public interface IGradeRepository
{
    Task<IEnumerable<GradeRecord>> GetAllAsync();
    Task<GradeRecord?> GetByIdAsync(Guid id);
    Task<IEnumerable<GradeRecord>> GetByStudentIdAsync(Guid studentId);
    Task<IEnumerable<GradeRecord>> GetByCourseIdAsync(Guid courseId);
    Task<bool> HasGradesByEducatorIdAsync(Guid educatorId);
    Task<GradeRecord> CreateAsync(GradeRecord grade);
    Task<GradeRecord> UpdateAsync(GradeRecord grade);
    Task<bool> DeleteAsync(Guid id);
}
