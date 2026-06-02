using StudentRegistrar.Models;

namespace StudentRegistrar.Data.Repositories;

public interface ICourseInstructorRepository
{
    Task<CourseInstructor?> GetByIdAsync(Guid id);
    Task<IEnumerable<CourseInstructor>> GetByCourseIdAsync(Guid courseId);
    Task<IEnumerable<CourseInstructor>> GetByAccountHolderIdAsync(Guid accountHolderId);
    Task<IEnumerable<CourseInstructor>> GetByInstructorEmailAsync(string email);
    Task<IEnumerable<CourseInstructor>> GetByEducatorIdAsync(Guid educatorId);
    Task<IEnumerable<CourseInstructor>> GetAllAsync();
    Task<CourseInstructor> CreateAsync(CourseInstructor courseInstructor);
    Task<CourseInstructor> UpdateAsync(CourseInstructor courseInstructor);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<CourseInstructor>> GetInstructorsForSemesterAsync(Guid semesterId);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> EmailExistsAsync(string email);
}
