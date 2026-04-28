using AutoMapper;
using FluentAssertions;
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
    private readonly EducatorService _service;

    public EducatorServiceTests()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _service = new EducatorService(
            _educatorRepository.Object,
            _accountHolderRepository.Object,
            _keycloakService.Object,
            mapperConfig.CreateMapper());
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

        result.Credentials.Should().NotBeNull();
        result.Credentials!.Username.Should().Be(request.Email);
        result.Credentials.TemporaryPassword.Should().Be("TempPass123!");
        result.Educator.Email.Should().Be(request.Email);
        result.Educator.KeycloakUserId.Should().Be(keycloakUserId);
        result.Educator.EducatorInfo.Department.Should().Be("STEM");

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

        result.Credentials.Should().BeNull();
        result.Educator.AccountHolderId.Should().Be(accountHolderId);
        result.Educator.KeycloakUserId.Should().Be(keycloakUserId);
        result.Educator.FirstName.Should().Be(accountHolder.FirstName);
        result.Educator.LastName.Should().Be(accountHolder.LastName);
        result.Educator.Email.Should().Be(accountHolder.EmailAddress);
        result.Educator.Phone.Should().Be(accountHolder.MobilePhone);
        result.Message.Should().Contain("authorized");

        _keycloakService.Verify(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
        _keycloakService.Verify(s => s.UpdateUserRoleAsync(keycloakUserId, UserRole.Educator), Times.Once);
        _educatorRepository.Verify(r => r.CreateAsync(It.Is<Educator>(e =>
            e.AccountHolderId == accountHolderId &&
            e.FirstName == accountHolder.FirstName &&
            e.LastName == accountHolder.LastName &&
            e.Email == accountHolder.EmailAddress &&
            e.Phone == accountHolder.MobilePhone)), Times.Once);
    }
}
