using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class EducatorServiceTests
{
    private readonly Mock<IEducatorRepository> _educatorRepository = new();
    private readonly Mock<IAccountHolderRepository> _accountHolderRepository = new();
    private readonly Mock<IKeycloakService> _keycloakService = new();
    private readonly Mock<IGradeRepository> _gradeRepository = new();
    private readonly EducatorService _service;

    public EducatorServiceTests()
    {
            var mapper = new ServiceCollection()
                .AddLogging()
                .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
                .BuildServiceProvider()
                .GetRequiredService<IMapper>();
            _service = new EducatorService(
                _educatorRepository.Object,
                _accountHolderRepository.Object,
                _keycloakService.Object,
                _gradeRepository.Object,
                mapper);
    }

    [Fact]
    public async Task InviteEducatorAsync_Should_Create_Keycloak_User_Assign_Educator_Role_And_Create_Educator()
    {
        var keycloakUserId = "keycloak-educator-1";
        var request = new InviteEducatorDto
        {
            FirstName = "External",
            LastName = "Teacher",
            Email = "external.teacher@example.com",
            Phone = "555-0100",
            EducatorInfo = new StudentRegistrar.Api.DTOs.EducatorInfo
            {
                Department = "STEM",
                Bio = "External educator"
            }
        };

        _keycloakService
            .Setup(s => s.CreateUserAsync(It.Is<CreateUserRequest>(r =>
                r.Email == request.Email && r.Role == UserRole.Educator)))
            .ReturnsAsync(new CreateUserResponse
            {
                UserId = keycloakUserId,
                Username = request.Email,
                TemporaryPassword = "TempPass123!",
                IsTemporary = true
            });

        _educatorRepository
            .Setup(r => r.CreateAsync(It.IsAny<Educator>()))
            .ReturnsAsync((Educator educator) => educator);

        var result = await _service.InviteEducatorAsync(request);

        Assert.NotNull(result.Credentials);
        Assert.Equal(request.Email, result.Credentials!.Username);
        Assert.Equal("TempPass123!", result.Credentials.TemporaryPassword);
        Assert.Equal(request.Email, result.Educator.Email);
        Assert.Equal(keycloakUserId, result.Educator.KeycloakUserId);
        Assert.Equal("STEM", result.Educator.EducatorInfo.Department);

        _keycloakService.Verify(s => s.UpdateUserRoleAsync(keycloakUserId, UserRole.Educator), Times.Once);
        _educatorRepository.Verify(r => r.CreateAsync(It.Is<Educator>(e =>
            e.KeycloakUserId == keycloakUserId &&
            e.AccountHolderId == null &&
            e.Email == request.Email &&
            e.IsActive)), Times.Once);
    }

    [Fact]
    public async Task InviteEducatorAsync_Should_Authorize_Existing_AccountHolder_As_Educator()
    {
        var accountHolderId = Guid.NewGuid();
        var keycloakUserId = "keycloak-parent-1";
        var accountHolder = new AccountHolder
        {
            Id = accountHolderId,
            FirstName = "Parent",
            LastName = "Teacher",
            EmailAddress = "parent.teacher@example.com",
            MobilePhone = "555-0111",
            KeycloakUserId = keycloakUserId
        };

        var request = new InviteEducatorDto
        {
            FirstName = "Spoofed",
            LastName = "Name",
            Email = "spoofed@example.com",
            Phone = "555-9999",
            AccountHolderId = accountHolderId
        };

        _accountHolderRepository
            .Setup(r => r.GetByIdAsync(accountHolderId))
            .ReturnsAsync(accountHolder);

        _keycloakService
            .Setup(s => s.GetUserIdByEmailAsync(accountHolder.EmailAddress))
            .ReturnsAsync(keycloakUserId);

        _educatorRepository
            .Setup(r => r.CreateAsync(It.IsAny<Educator>()))
            .ReturnsAsync((Educator educator) => educator);

        var result = await _service.InviteEducatorAsync(request);

        Assert.Null(result.Credentials);
        Assert.Equal(accountHolderId, result.Educator.AccountHolderId);
        Assert.Equal(keycloakUserId, result.Educator.KeycloakUserId);
        Assert.Equal(accountHolder.FirstName, result.Educator.FirstName);
        Assert.Equal(accountHolder.LastName, result.Educator.LastName);
        Assert.Equal(accountHolder.EmailAddress, result.Educator.Email);
        Assert.Equal(accountHolder.MobilePhone, result.Educator.Phone);
        Assert.Contains("authorized", result.Message);

        _keycloakService.Verify(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
        _keycloakService.Verify(s => s.UpdateUserRoleAsync(keycloakUserId, UserRole.Educator), Times.Once);
        _educatorRepository.Verify(r => r.CreateAsync(It.Is<Educator>(e =>
            e.AccountHolderId == accountHolderId &&
            e.FirstName == accountHolder.FirstName &&
            e.LastName == accountHolder.LastName &&
            e.Email == accountHolder.EmailAddress &&
            e.Phone == accountHolder.MobilePhone)), Times.Once);
    }

    [Fact]
    public async Task InviteEducatorAsync_Should_Create_Keycloak_User_For_AccountHolder_Without_Login()
    {
        var accountHolderId = Guid.NewGuid();
        var keycloakUserId = "keycloak-new-parent-educator";
        var accountHolder = new AccountHolder
        {
            Id = accountHolderId,
            FirstName = "Parent",
            LastName = "WithoutLogin",
            EmailAddress = "parent.without.login@example.com",
            HomePhone = "555-0112",
            KeycloakUserId = string.Empty
        };

        var request = new InviteEducatorDto
        {
            AccountHolderId = accountHolderId
        };

        _accountHolderRepository
            .Setup(r => r.GetByIdAsync(accountHolderId))
            .ReturnsAsync(accountHolder);

        _keycloakService
            .Setup(s => s.GetUserIdByEmailAsync(accountHolder.EmailAddress))
            .ReturnsAsync((string?)null);

        _keycloakService
            .Setup(s => s.CreateUserAsync(It.Is<CreateUserRequest>(r =>
                r.Email == accountHolder.EmailAddress &&
                r.FirstName == accountHolder.FirstName &&
                r.LastName == accountHolder.LastName &&
                r.Role == UserRole.Educator)))
            .ReturnsAsync(new CreateUserResponse
            {
                UserId = keycloakUserId,
                Username = accountHolder.EmailAddress,
                TemporaryPassword = "TempPass123!",
                IsTemporary = false
            });

        _accountHolderRepository
            .Setup(r => r.UpdateAsync(accountHolder))
            .ReturnsAsync(accountHolder);

        _educatorRepository
            .Setup(r => r.CreateAsync(It.IsAny<Educator>()))
            .ReturnsAsync((Educator educator) => educator);

        var result = await _service.InviteEducatorAsync(request);

        Assert.NotNull(result.Credentials);
        Assert.Equal(accountHolder.EmailAddress, result.Credentials!.Username);
        Assert.Equal("TempPass123!", result.Credentials.TemporaryPassword);
        Assert.Equal(accountHolderId, result.Educator.AccountHolderId);
        Assert.Equal(keycloakUserId, result.Educator.KeycloakUserId);
        Assert.Equal(keycloakUserId, accountHolder.KeycloakUserId);

        _accountHolderRepository.Verify(r => r.UpdateAsync(It.Is<AccountHolder>(a =>
            a.Id == accountHolderId &&
            a.KeycloakUserId == keycloakUserId)), Times.Once);
        _keycloakService.Verify(s => s.UpdateUserRoleAsync(keycloakUserId, UserRole.Educator), Times.Once);
        _educatorRepository.Verify(r => r.CreateAsync(It.Is<Educator>(e =>
            e.AccountHolderId == accountHolderId &&
            e.KeycloakUserId == keycloakUserId &&
            e.Email == accountHolder.EmailAddress)), Times.Once);
    }

    [Fact]
    public async Task InviteEducatorAsync_Should_Update_Existing_Educator_For_AccountHolder()
    {
        var accountHolderId = Guid.NewGuid();
        var keycloakUserId = "keycloak-parent-1";
        var accountHolder = new AccountHolder
        {
            Id = accountHolderId,
            FirstName = "Parent",
            LastName = "Teacher",
            EmailAddress = "parent.teacher@example.com",
            MobilePhone = "555-0111",
            KeycloakUserId = keycloakUserId
        };
        var existingEducator = new Educator
        {
            Id = Guid.NewGuid(),
            AccountHolderId = accountHolderId,
            FirstName = accountHolder.FirstName,
            LastName = accountHolder.LastName,
            Email = accountHolder.EmailAddress,
            IsActive = false
        };

        var request = new InviteEducatorDto
        {
            AccountHolderId = accountHolderId,
            EducatorInfo = new StudentRegistrar.Api.DTOs.EducatorInfo
            {
                Department = "Science",
                Bio = "Returning educator"
            }
        };

        _accountHolderRepository
            .Setup(r => r.GetByIdAsync(accountHolderId))
            .ReturnsAsync(accountHolder);

        _keycloakService
            .Setup(s => s.GetUserIdByEmailAsync(accountHolder.EmailAddress))
            .ReturnsAsync(keycloakUserId);

        _educatorRepository
            .Setup(r => r.GetByAccountHolderIdAsync(accountHolderId))
            .ReturnsAsync(existingEducator);

        _educatorRepository
            .Setup(r => r.UpdateAsync(existingEducator))
            .ReturnsAsync(existingEducator);

        var result = await _service.InviteEducatorAsync(request);

        Assert.Null(result.Credentials);
        Assert.Contains("authorized", result.Message);
        Assert.Equal(existingEducator.Id, result.Educator.Id);
        Assert.True(result.Educator.IsActive);
        Assert.Equal(keycloakUserId, result.Educator.KeycloakUserId);
        Assert.Equal("Science", result.Educator.EducatorInfo.Department);

        _keycloakService.Verify(s => s.UpdateUserRoleAsync(keycloakUserId, UserRole.Educator), Times.Once);
        _educatorRepository.Verify(r => r.CreateAsync(It.IsAny<Educator>()), Times.Never);
        _educatorRepository.Verify(r => r.UpdateAsync(existingEducator), Times.Once);
    }

    [Fact]
    public async Task DeleteEducatorAsync_Should_HardDelete_When_No_Grade_History()
    {
        var educatorId = Guid.NewGuid();

        _gradeRepository
            .Setup(r => r.HasGradesByEducatorIdAsync(educatorId))
            .ReturnsAsync(false);

        _educatorRepository
            .Setup(r => r.DeleteAsync(educatorId))
            .ReturnsAsync(true);

        var result = await _service.DeleteEducatorAsync(educatorId);

        Assert.Equal(DeleteEducatorResult.HardDeleted, result);
        _educatorRepository.Verify(r => r.DeleteAsync(educatorId), Times.Once);
        _educatorRepository.Verify(r => r.SoftDeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEducatorAsync_Should_SoftDelete_When_Grade_History_Exists()
    {
        var educatorId = Guid.NewGuid();

        _gradeRepository
            .Setup(r => r.HasGradesByEducatorIdAsync(educatorId))
            .ReturnsAsync(true);

        _educatorRepository
            .Setup(r => r.SoftDeleteAsync(educatorId))
            .ReturnsAsync(true);

        var result = await _service.DeleteEducatorAsync(educatorId);

        Assert.Equal(DeleteEducatorResult.SoftDeleted, result);
        _educatorRepository.Verify(r => r.SoftDeleteAsync(educatorId), Times.Once);
        _educatorRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEducatorAsync_Should_Return_NotFound_When_Educator_Does_Not_Exist()
    {
        var educatorId = Guid.NewGuid();

        _gradeRepository
            .Setup(r => r.HasGradesByEducatorIdAsync(educatorId))
            .ReturnsAsync(false);

        _educatorRepository
            .Setup(r => r.DeleteAsync(educatorId))
            .ReturnsAsync(false);

        var result = await _service.DeleteEducatorAsync(educatorId);

        Assert.Equal(DeleteEducatorResult.NotFound, result);
    }
}
