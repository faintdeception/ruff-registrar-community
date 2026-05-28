using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
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

        Assert.NotNull(result);
        Assert.Equal(accountHolder.Id.ToString(), result!.Id);
        Assert.Equal(accountHolder.EmailAddress, result.EmailAddress);

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

        Assert.Null(result);
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

        Assert.Equal(2, result.Count());
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

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAccountHolderByUserIdAsync_WhenNotFound_ReturnsNull()
    {
        _accountHolderRepository.Setup(r => r.GetByKeycloakUserIdAsync(It.IsAny<string>())).ReturnsAsync((AccountHolder?)null);

        var result = await _service.GetAccountHolderByUserIdAsync("kc-missing");

        Assert.Null(result);
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

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAccountHolderByIdAsync_WhenNotFound_ReturnsNull()
    {
        _accountHolderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AccountHolder?)null);

        var result = await _service.GetAccountHolderByIdAsync(Guid.NewGuid());

        Assert.Null(result);
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

        Assert.NotNull(result);
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

        Assert.NotNull(result);
        _accountHolderRepository.Verify(r => r.UpdateAsync(It.IsAny<AccountHolder>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAccountHolderAsync_WhenNotFound_ReturnsNull()
    {
        _accountHolderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AccountHolder?)null);

        var result = await _service.UpdateAccountHolderAsync(Guid.NewGuid(), new UpdateAccountHolderDto());

        Assert.Null(result);
        _accountHolderRepository.Verify(r => r.UpdateAsync(It.IsAny<AccountHolder>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // RemoveStudentFromAccountAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveStudentFromAccountAsync_WhenStudentNotFound_ReturnsFalse()
    {
        _studentRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Student?)null);

        var result = await _service.RemoveStudentFromAccountAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result);
    }
}
