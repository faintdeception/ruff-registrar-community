using AutoMapper;
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
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _service = new AccountHolderService(
            _accountHolderRepository.Object,
            _studentRepository.Object,
            mapperConfig.CreateMapper());
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
}
