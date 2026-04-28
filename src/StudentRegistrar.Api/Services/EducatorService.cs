using AutoMapper;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class EducatorService : IEducatorService
{
    private readonly IEducatorRepository _educatorRepository;
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly IKeycloakService _keycloakService;
    private readonly IMapper _mapper;

    public EducatorService(
        IEducatorRepository educatorRepository,
        IAccountHolderRepository accountHolderRepository,
        IKeycloakService keycloakService,
        IMapper mapper)
    {
        _educatorRepository = educatorRepository;
        _accountHolderRepository = accountHolderRepository;
        _keycloakService = keycloakService;
        _mapper = mapper;
    }

    public async Task<IEnumerable<EducatorDto>> GetAllEducatorsAsync()
    {
        var educators = await _educatorRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<EducatorDto>>(educators);
    }

    public async Task<EducatorDto?> GetEducatorByIdAsync(Guid id)
    {
        var educator = await _educatorRepository.GetByIdAsync(id);
        return educator == null ? null : _mapper.Map<EducatorDto>(educator);
    }

    public async Task<EducatorDto> CreateEducatorAsync(CreateEducatorDto createDto)
    {
        var educator = _mapper.Map<Educator>(createDto);
        educator.IsActive = true; // Set as active by default
        var createdEducator = await _educatorRepository.CreateAsync(educator);
        return _mapper.Map<EducatorDto>(createdEducator);
    }

    public async Task<InviteEducatorResponse> InviteEducatorAsync(InviteEducatorDto inviteDto)
    {
        CreateUserResponse? createdUser = null;
        string? keycloakUserId;
        Guid? accountHolderId = inviteDto.AccountHolderId;
        string firstName = inviteDto.FirstName;
        string lastName = inviteDto.LastName;
        string email = inviteDto.Email;
        string? phone = inviteDto.Phone;

        if (inviteDto.AccountHolderId.HasValue)
        {
            var accountHolder = await _accountHolderRepository.GetByIdAsync(inviteDto.AccountHolderId.Value)
                ?? throw new InvalidOperationException("Account holder was not found.");

            firstName = accountHolder.FirstName;
            lastName = accountHolder.LastName;
            email = accountHolder.EmailAddress;
            phone = accountHolder.MobilePhone ?? accountHolder.HomePhone ?? inviteDto.Phone;

            keycloakUserId = await _keycloakService.GetUserIdByEmailAsync(accountHolder.EmailAddress);
            if (string.IsNullOrWhiteSpace(keycloakUserId) && !string.IsNullOrWhiteSpace(accountHolder.KeycloakUserId))
            {
                keycloakUserId = accountHolder.KeycloakUserId;
            }

            if (string.IsNullOrWhiteSpace(keycloakUserId))
            {
                throw new InvalidOperationException("Account holder does not have a Keycloak user.");
            }
        }
        else
        {
            createdUser = await _keycloakService.CreateUserAsync(new CreateUserRequest
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = UserRole.Educator,
                Password = string.Empty,
                RequirePasswordChange = false,
                RequireEmailVerification = false
            });

            keycloakUserId = createdUser.UserId;
        }

        await _keycloakService.UpdateUserRoleAsync(keycloakUserId, UserRole.Educator);

        if (accountHolderId.HasValue)
        {
            var existingEducator = await _educatorRepository.GetByAccountHolderIdAsync(accountHolderId.Value);
            if (existingEducator != null)
            {
                existingEducator.FirstName = firstName;
                existingEducator.LastName = lastName;
                existingEducator.Email = email;
                existingEducator.Phone = phone;
                existingEducator.KeycloakUserId = keycloakUserId;
                existingEducator.IsActive = true;
                existingEducator.UpdatedAt = DateTime.UtcNow;

                if (inviteDto.EducatorInfo != null)
                {
                    existingEducator.SetEducatorInfo(new StudentRegistrar.Models.EducatorInfo
                    {
                        Bio = inviteDto.EducatorInfo.Bio,
                        Qualifications = inviteDto.EducatorInfo.Qualifications,
                        Specializations = inviteDto.EducatorInfo.Specializations,
                        Department = inviteDto.EducatorInfo.Department,
                        CustomFields = inviteDto.EducatorInfo.CustomFields
                    });
                }

                var updatedEducator = await _educatorRepository.UpdateAsync(existingEducator);
                return new InviteEducatorResponse
                {
                    Educator = _mapper.Map<EducatorDto>(updatedEducator),
                    Credentials = null,
                    Message = "Educator authorized successfully."
                };
            }
        }

        var educator = new Educator
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            AccountHolderId = accountHolderId,
            KeycloakUserId = keycloakUserId,
            IsActive = true
        };

        if (inviteDto.EducatorInfo != null)
        {
            educator.SetEducatorInfo(new StudentRegistrar.Models.EducatorInfo
            {
                Bio = inviteDto.EducatorInfo.Bio,
                Qualifications = inviteDto.EducatorInfo.Qualifications,
                Specializations = inviteDto.EducatorInfo.Specializations,
                Department = inviteDto.EducatorInfo.Department,
                CustomFields = inviteDto.EducatorInfo.CustomFields
            });
        }

        var createdEducator = await _educatorRepository.CreateAsync(educator);

        return new InviteEducatorResponse
        {
            Educator = _mapper.Map<EducatorDto>(createdEducator),
            Credentials = createdUser != null
                ? new UserCredentials
                {
                    Username = createdUser.Username,
                    TemporaryPassword = createdUser.TemporaryPassword ?? string.Empty,
                    MustChangePassword = createdUser.IsTemporary
                }
                : null,
            Message = createdUser != null
                ? "Educator invited successfully."
                : "Educator authorized successfully."
        };
    }

    public async Task<EducatorDto?> UpdateEducatorAsync(Guid id, UpdateEducatorDto updateDto)
    {
        var existingEducator = await _educatorRepository.GetByIdAsync(id);
        if (existingEducator == null)
            return null;

        _mapper.Map(updateDto, existingEducator);
        var updatedEducator = await _educatorRepository.UpdateAsync(existingEducator);
        return _mapper.Map<EducatorDto>(updatedEducator);
    }

    public async Task<bool> DeleteEducatorAsync(Guid id)
    {
        return await _educatorRepository.DeleteAsync(id);
    }

    public async Task<bool> DeactivateEducatorAsync(Guid id)
    {
        return await _educatorRepository.DeactivateAsync(id);
    }

    public async Task<bool> ActivateEducatorAsync(Guid id)
    {
        return await _educatorRepository.ActivateAsync(id);
    }
}
