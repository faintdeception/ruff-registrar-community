using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface IStudentService
{
    Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
    Task<StudentDto?> GetStudentByIdAsync(Guid id);
    Task<StudentDto> CreateStudentAsync(CreateStudentDto createStudentDto);
    Task<StudentDto?> UpdateStudentAsync(Guid id, UpdateStudentDto updateStudentDto);
    Task<bool> DeleteStudentAsync(Guid id);
    Task<IEnumerable<StudentDto>> GetStudentsByAccountHolderAsync(Guid accountHolderId);
}
