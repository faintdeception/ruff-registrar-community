using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class AccountHolderServiceTests
{
    private readonly Mock<IAccountHolderRepository> _accountHolderRepository = new();
    private readonly Mock<IStudentRepository> _studentRepository = new();
    private readonly AccountHolderService _service;

    public AccountHolderServiceTests()
    {
            var mapper = new ServiceCollection()
                .AddLogging()
                .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
                .BuildServiceProvider()
                .GetRequiredService<IMapper>();
            _service = new AccountHolderService(
                _accountHolderRepository.Object,
                _studentRepository.Object,
                mapper);
    }

    [Fact]
    public async Task LinkAccountHolderToUserAsync_Should_Update_Keycloak_User_Id_For_Email_Match()
    {
        var accountHolder = new AccountHolder
        {
            Id = Guid.NewGuid(),
            FirstName = "Mark",
            LastName = "Member",
            EmailAddress = "mark.member@example.com",
            KeycloakUserId = "seed-placeholder-id"
        };

        _accountHolderRepository
            .Setup(r => r.GetByEmailAsync(accountHolder.EmailAddress))
            .ReturnsAsync(accountHolder);

        _accountHolderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AccountHolder>()))
            .ReturnsAsync((AccountHolder updatedAccountHolder) => updatedAccountHolder);

        var result = await _service.LinkAccountHolderToUserAsync(accountHolder.EmailAddress, "current-token-sub");

        result.Should().NotBeNull();
        result!.Id.Should().Be(accountHolder.Id.ToString());
        result.EmailAddress.Should().Be(accountHolder.EmailAddress);

        _accountHolderRepository.Verify(r => r.UpdateAsync(It.Is<AccountHolder>(a =>
            a.Id == accountHolder.Id &&
            a.KeycloakUserId == "current-token-sub")), Times.Once);
    }

    [Fact]
    public async Task LinkAccountHolderToUserAsync_Should_Return_Null_When_Email_Is_Unknown()
    {
        _accountHolderRepository
            .Setup(r => r.GetByEmailAsync("missing@example.com"))
            .ReturnsAsync((AccountHolder?)null);

        var result = await _service.LinkAccountHolderToUserAsync("missing@example.com", "current-token-sub");

        result.Should().BeNull();
        _accountHolderRepository.Verify(r => r.UpdateAsync(It.IsAny<AccountHolder>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // GetAllAccountHoldersAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAccountHoldersAsync_ReturnsMappedDtos()
    {
        var holders = new List<AccountHolder>
        {
            new() { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", EmailAddress = "jane@example.com", KeycloakUserId = "kc-1", AddressJson = "{}", EmergencyContactJson = "{}" },
            new() { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), FirstName = "Bob", LastName = "Smith", EmailAddress = "bob@example.com", KeycloakUserId = "kc-2", AddressJson = "{}", EmergencyContactJson = "{}" }
        };
        _accountHolderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(holders);

        var result = await _service.GetAllAccountHoldersAsync();

        result.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // GetAccountHolderByUserIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAccountHolderByUserIdAsync_WhenFound_ReturnsDto()
    {
        var holder = new AccountHolder { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", EmailAddress = "jane@example.com", KeycloakUserId = "kc-abc", AddressJson = "{}", EmergencyContactJson = "{}" };
        _accountHolderRepository.Setup(r => r.GetByKeycloakUserIdAsync("kc-abc")).ReturnsAsync(holder);

        var result = await _service.GetAccountHolderByUserIdAsync("kc-abc");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAccountHolderByUserIdAsync_WhenNotFound_ReturnsNull()
    {
        _accountHolderRepository.Setup(r => r.GetByKeycloakUserIdAsync(It.IsAny<string>())).ReturnsAsync((AccountHolder?)null);

        var result = await _service.GetAccountHolderByUserIdAsync("kc-missing");

        result.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // GetAccountHolderByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAccountHolderByIdAsync_WhenFound_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var holder = new AccountHolder { Id = id, TenantId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", EmailAddress = "jane@example.com", KeycloakUserId = "kc-1", AddressJson = "{}", EmergencyContactJson = "{}" };
        _accountHolderRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(holder);

        var result = await _service.GetAccountHolderByIdAsync(id);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAccountHolderByIdAsync_WhenNotFound_ReturnsNull()
    {
        _accountHolderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AccountHolder?)null);

        var result = await _service.GetAccountHolderByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // CreateAccountHolderAsync (with keycloakUserId)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAccountHolderAsync_WithKeycloakId_SetsKeycloakIdOnEntity()
    {
        const string keycloakId = "kc-brand-new";
        var dto = new CreateAccountHolderDto { FirstName = "New", LastName = "Member", EmailAddress = "new@example.com" };
        var created = new AccountHolder { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), FirstName = "New", LastName = "Member", EmailAddress = "new@example.com", KeycloakUserId = keycloakId, AddressJson = "{}", EmergencyContactJson = "{}" };
        _accountHolderRepository.Setup(r => r.CreateAsync(It.IsAny<AccountHolder>())).ReturnsAsync(created);

        var result = await _service.CreateAccountHolderAsync(dto, keycloakId);

        result.Should().NotBeNull();
        _accountHolderRepository.Verify(
            r => r.CreateAsync(It.Is<AccountHolder>(h => h.KeycloakUserId == keycloakId)),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateAccountHolderAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAccountHolderAsync_WhenFound_ReturnsUpdatedDto()
    {
        var id = Guid.NewGuid();
        var existing = new AccountHolder { Id = id, TenantId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", EmailAddress = "jane@example.com", KeycloakUserId = "kc-1", AddressJson = "{}", EmergencyContactJson = "{}" };
        var updated = new AccountHolder { Id = id, TenantId = Guid.NewGuid(), FirstName = "Janet", LastName = "Doe", EmailAddress = "jane@example.com", KeycloakUserId = "kc-1", AddressJson = "{}", EmergencyContactJson = "{}" };
        _accountHolderRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _accountHolderRepository.Setup(r => r.UpdateAsync(It.IsAny<AccountHolder>())).ReturnsAsync(updated);

        var result = await _service.UpdateAccountHolderAsync(id, new UpdateAccountHolderDto { FirstName = "Janet" });

        result.Should().NotBeNull();
        _accountHolderRepository.Verify(r => r.UpdateAsync(It.IsAny<AccountHolder>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAccountHolderAsync_WhenNotFound_ReturnsNull()
    {
        _accountHolderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AccountHolder?)null);

        var result = await _service.UpdateAccountHolderAsync(Guid.NewGuid(), new UpdateAccountHolderDto());

        result.Should().BeNull();
        _accountHolderRepository.Verify(r => r.UpdateAsync(It.IsAny<AccountHolder>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // AddStudentToAccountAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddStudentToAccountAsync_MapsStudentInfoAndNotes_ForAccountWorkflow()
    {
        var accountHolderId = Guid.NewGuid();
        var createDto = new CreateStudentForAccountDto
        {
            FirstName = "Avery",
            LastName = "Learner",
            Grade = "5",
            DateOfBirth = new DateTime(2014, 9, 12),
            Notes = "Needs a peanut-free classroom.",
            StudentInfoJson = new StudentInfoDetails
            {
                SpecialConditions = new() { "ADHD" },
                Allergies = new() { "Peanuts" },
                Medications = new() { "Inhaler" },
                PreferredName = "Ave",
                ParentNotes = "Call before administering medication."
            }
        };

        _studentRepository
            .Setup(r => r.CreateAsync(It.IsAny<Student>()))
            .ReturnsAsync((Student student) => student);

        var result = await _service.AddStudentToAccountAsync(accountHolderId, createDto);

        result.FirstName.Should().Be("Avery");
        result.LastName.Should().Be("Learner");

        _studentRepository.Verify(r => r.CreateAsync(It.Is<Student>(student =>
            student.AccountHolderId == accountHolderId &&
            student.FirstName == createDto.FirstName &&
            student.LastName == createDto.LastName &&
            student.Grade == createDto.Grade &&
            student.DateOfBirth == createDto.DateOfBirth &&
            student.Notes == createDto.Notes &&
            student.GetStudentInfo().SpecialConditions.SequenceEqual(new[] { "ADHD" }) &&
            student.GetStudentInfo().Allergies.SequenceEqual(new[] { "Peanuts" }) &&
            student.GetStudentInfo().Medications.SequenceEqual(new[] { "Inhaler" }) &&
            student.GetStudentInfo().PreferredName == "Ave" &&
            student.GetStudentInfo().ParentNotes == "Call before administering medication.")), Times.Once);
    }

    // -------------------------------------------------------------------------
    // RemoveStudentFromAccountAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveStudentFromAccountAsync_WhenStudentNotFound_ReturnsFalse()
    {
        _studentRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Student?)null);

        var result = await _service.RemoveStudentFromAccountAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }
}
