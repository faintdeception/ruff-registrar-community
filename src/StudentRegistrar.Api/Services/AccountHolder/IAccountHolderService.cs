using StudentRegistrar.Api.DTOs;

namespace StudentRegistrar.Api.Services;

public interface IAccountHolderService
{
    Task<IEnumerable<AccountHolderDto>> GetAllAccountHoldersAsync();
    Task<AccountHolderDto?> GetAccountHolderByUserIdAsync(string userId);
    Task<AccountHolderDto?> GetAccountHolderByIdAsync(Guid id);
    Task<AccountHolderDto> CreateAccountHolderAsync(CreateAccountHolderDto createDto);
    Task<AccountHolderDto> CreateAccountHolderAsync(CreateAccountHolderDto createDto, string? keycloakUserId);
    Task<AccountHolderDto?> UpdateAccountHolderAsync(Guid id, UpdateAccountHolderDto updateDto);
    Task<StudentDto> AddStudentToAccountAsync(Guid accountHolderId, CreateStudentForAccountDto createDto);
    Task<bool> RemoveStudentFromAccountAsync(Guid accountHolderId, Guid studentId);
}
