using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class AccountHolderService : IAccountHolderService
{
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IMapper _mapper;

    public AccountHolderService(
        IAccountHolderRepository accountHolderRepository,
        IStudentRepository studentRepository,
        IMapper mapper)
    {
        _accountHolderRepository = accountHolderRepository;
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AccountHolderDto>> GetAllAccountHoldersAsync()
    {
        var accountHolders = await _accountHolderRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<AccountHolderDto>>(accountHolders);
    }

    public async Task<AccountHolderDto?> GetAccountHolderByUserIdAsync(string userId)
    {
        var accountHolder = await _accountHolderRepository.GetByKeycloakUserIdAsync(userId);
        return accountHolder != null ? _mapper.Map<AccountHolderDto>(accountHolder) : null;
    }

    public async Task<AccountHolderDto?> GetAccountHolderByIdAsync(Guid id)
    {
        var accountHolder = await _accountHolderRepository.GetByIdAsync(id);
        return accountHolder != null ? _mapper.Map<AccountHolderDto>(accountHolder) : null;
    }

    public async Task<AccountHolderDto> CreateAccountHolderAsync(CreateAccountHolderDto createDto)
    {
        var accountHolder = _mapper.Map<AccountHolder>(createDto);
        var createdAccountHolder = await _accountHolderRepository.CreateAsync(accountHolder);
        return _mapper.Map<AccountHolderDto>(createdAccountHolder);
    }

    public async Task<AccountHolderDto> CreateAccountHolderAsync(CreateAccountHolderDto createDto, string? keycloakUserId)
    {
        var accountHolder = _mapper.Map<AccountHolder>(createDto);
        if (!string.IsNullOrEmpty(keycloakUserId))
        {
            accountHolder.KeycloakUserId = keycloakUserId;
        }
        var createdAccountHolder = await _accountHolderRepository.CreateAsync(accountHolder);
        return _mapper.Map<AccountHolderDto>(createdAccountHolder);
    }

    public async Task<AccountHolderDto?> UpdateAccountHolderAsync(Guid id, UpdateAccountHolderDto updateDto)
    {
        var existingAccountHolder = await _accountHolderRepository.GetByIdAsync(id);
        if (existingAccountHolder == null)
            return null;

        _mapper.Map(updateDto, existingAccountHolder);
        var updatedAccountHolder = await _accountHolderRepository.UpdateAsync(existingAccountHolder);
        return _mapper.Map<AccountHolderDto>(updatedAccountHolder);
    }

    public async Task<StudentDto> AddStudentToAccountAsync(Guid accountHolderId, CreateStudentForAccountDto createDto)
    {
        var student = _mapper.Map<Student>(createDto);
        student.AccountHolderId = accountHolderId;
        
        var createdStudent = await _studentRepository.CreateAsync(student);
        return _mapper.Map<StudentDto>(createdStudent);
    }

    public async Task<bool> RemoveStudentFromAccountAsync(Guid accountHolderId, Guid studentId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null || student.AccountHolderId != accountHolderId)
            return false;

        return await _studentRepository.DeleteAsync(studentId);
    }
}
