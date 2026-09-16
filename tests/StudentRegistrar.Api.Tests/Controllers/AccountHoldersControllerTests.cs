using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class AccountHoldersControllerTests
{
    private readonly Mock<IAccountHolderService> _accountHolderService = new();
    private readonly Mock<IKeycloakService> _keycloakService = new();
    private readonly Mock<ITenantSettingsService> _tenantSettingsService = new();
    private readonly AccountHoldersController _controller;

    public AccountHoldersControllerTests()
    {
        _controller = new AccountHoldersController(
            _accountHolderService.Object,
            _keycloakService.Object,
            _tenantSettingsService.Object,
            NullLogger<AccountHoldersController>.Instance);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    /// <summary>
    /// Sets up the ClaimsPrincipal on the controller's HttpContext with an optional role claim.
    /// </summary>
    private void SetUser(string keycloakId = "kc-user-1", string email = "user@example.com",
        string firstName = "Jane", string lastName = "Doe", string? role = null)
    {
        var claims = new List<Claim>
        {
            new("sub", keycloakId),
            new(ClaimTypes.NameIdentifier, keycloakId),
            new(ClaimTypes.Email, email),
            new("email", email),
            new(ClaimTypes.GivenName, firstName),
            new("given_name", firstName),
            new(ClaimTypes.Surname, lastName),
            new("family_name", lastName),
        };

        if (role != null)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    private static AccountHolderDto MakeDto(string id = "", string email = "user@example.com",
        string firstName = "Jane", string lastName = "Doe")
        => new()
        {
            Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id,
            FirstName = firstName,
            LastName = lastName,
            EmailAddress = email
        };

    private static StudentDto MakeStudentDto()
        => new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Child",
            LastName = "Doe",
            Email = "child@example.com",
            DateOfBirth = new DateOnly(2015, 1, 1)
        };

    // -------------------------------------------------------------------------
    // GET /api/accountholders (admin)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAccountHolders_ReturnsOkWithList()
    {
        SetUser(role: "Administrator");
        var dtos = new List<AccountHolderDto> { MakeDto(), MakeDto(email: "b@example.com") };
        _accountHolderService.Setup(s => s.GetAllAccountHoldersAsync()).ReturnsAsync(dtos);

        var result = await _controller.GetAllAccountHolders();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dtos, ok.Value);
    }

    // -------------------------------------------------------------------------
    // GET /api/accountholders/{id} (admin)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAccountHolder_WhenFound_ReturnsOk()
    {
        SetUser(role: "Administrator");
        var id = Guid.NewGuid();
        var dto = MakeDto(id: id.ToString());
        _accountHolderService.Setup(s => s.GetAccountHolderByIdAsync(id)).ReturnsAsync(dto);

        var result = await _controller.GetAccountHolder(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task GetAccountHolder_WhenNotFound_ReturnsNotFound()
    {
        SetUser(role: "Administrator");
        _accountHolderService.Setup(s => s.GetAccountHolderByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AccountHolderDto?)null);

        var result = await _controller.GetAccountHolder(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -------------------------------------------------------------------------
    // GET /api/accountholders/me
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMyAccountHolder_WhenAccountHolderFound_ReturnsOk()
    {
        SetUser(keycloakId: "kc-user-1");
        var dto = MakeDto();
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync("kc-user-1")).ReturnsAsync(dto);

        var result = await _controller.GetMyAccountHolder();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task GetMyAccountHolder_WhenNotFoundButEmailLinks_ReturnsOk()
    {
        SetUser(keycloakId: "kc-user-2", email: "linked@example.com");
        var dto = MakeDto(email: "linked@example.com");
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync("kc-user-2")).ReturnsAsync((AccountHolderDto?)null);
        _accountHolderService.Setup(s => s.LinkAccountHolderToUserAsync("linked@example.com", "kc-user-2")).ReturnsAsync(dto);

        var result = await _controller.GetMyAccountHolder();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    // -------------------------------------------------------------------------
    // POST /api/accountholders (admin)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAccountHolder_WhenAdmin_Returns201WithResponse()
    {
        SetUser(role: "Administrator", email: "admin@example.com");
        var createDto = new CreateAccountHolderDto
        {
            FirstName = "New",
            LastName = "Member",
            EmailAddress = "new@example.com"
        };
        var createdDto = MakeDto(id: Guid.NewGuid().ToString(), email: "new@example.com");
        var keycloakResponse = new CreateUserResponse
        {
            UserId = "kc-new-user",
            Username = "new@example.com",
            IsTemporary = true,
            TemporaryPassword = "TempPass1!"
        };
        _keycloakService.Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>())).ReturnsAsync(keycloakResponse);
        _accountHolderService.Setup(s => s.CreateAccountHolderAsync(createDto, "kc-new-user")).ReturnsAsync(createdDto);

        var result = await _controller.CreateAccountHolder(createDto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(nameof(_controller.GetAccountHolder), created.ActionName);
        var response = Assert.IsType<CreateAccountHolderResponse>(created.Value);
        Assert.Same(createdDto, response.AccountHolder);
    }

    // -------------------------------------------------------------------------
    // POST /api/accountholders/bulk-import (admin, family .xlsx)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BulkImportFamilies_NoFile_ReturnsBadRequest()
    {
        SetUser(role: "Administrator");

        var result = await _controller.BulkImportFamilies(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task BulkImportFamilies_PasswordNotConfigured_ReturnsBadRequestWithoutTouchingKeycloak()
    {
        SetUser(role: "Administrator");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InsecureInitialInvitePasswordException(new[] { "no initial invite password has been configured yet" }));

        var file = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Doe", "jane@example.com", "Alex", "Doe", null, null }
        });

        var result = await _controller.BulkImportFamilies(file, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _keycloakService.Verify(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
    }

    [Fact]
    public async Task BulkImportFamilies_InvalidWorkbook_ReturnsBadRequest()
    {
        SetUser(role: "Administrator");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Correct-Horse-99");

        var badHeaders = FamilyImportTemplateGenerator.Headers.Where(h => h != "ChildGrade").ToArray();
        var file = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Doe", "jane@example.com", "Alex", "Doe", null }
        }, badHeaders);

        var result = await _controller.BulkImportFamilies(file, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task BulkImportFamilies_NewParentEmail_CreatesAccountAndAddsAllChildren()
    {
        SetUser(role: "Administrator", email: "admin@example.com");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Correct-Horse-99");

        var file = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", new DateTime(2015, 4, 12), "3rd" },
            new object?[] { "Jane", "Smith", "jane@example.com", "Sam", "Smith", null, "K" },
        });

        var newAccountId = Guid.NewGuid();
        _accountHolderService.Setup(s => s.GetAccountHolderByEmailAsync("jane@example.com")).ReturnsAsync((AccountHolderDto?)null);
        _keycloakService
            .Setup(s => s.CreateUserAsync(It.Is<CreateUserRequest>(r => r.Email == "jane@example.com")))
            .ReturnsAsync(new CreateUserResponse { UserId = "kc-jane", Username = "jane@example.com", IsTemporary = true, TemporaryPassword = "Correct-Horse-99" });
        _accountHolderService
            .Setup(s => s.CreateAccountHolderAsync(It.IsAny<CreateAccountHolderDto>(), "kc-jane"))
            .ReturnsAsync(MakeDto(id: newAccountId.ToString(), email: "jane@example.com"));
        _accountHolderService
            .Setup(s => s.AddStudentToAccountAsync(newAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ReturnsAsync(MakeStudentDto);

        var result = await _controller.BulkImportFamilies(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FamilyImportResponse>(ok.Value);

        Assert.Equal(1, response.TotalFamilies);
        Assert.Equal(2, response.TotalChildren);
        Assert.Equal(1, response.SuccessCount);
        Assert.Equal(0, response.FailureCount);

        var family = Assert.Single(response.Families);
        Assert.True(family.ParentAccountCreated);
        Assert.True(family.Success);
        Assert.Equal(2, family.Children.Count);

        _accountHolderService.Verify(s => s.AddStudentToAccountAsync(newAccountId, It.IsAny<CreateStudentForAccountDto>()), Times.Exactly(2));
        _keycloakService.Verify(
            s => s.CreateUserAsync(It.Is<CreateUserRequest>(r => !r.RequireEmailVerification)),
            Times.Once);
    }

    [Fact]
    public async Task BulkImportFamilies_ExistingParentEmail_AddsChildrenWithoutCreatingKeycloakUser()
    {
        SetUser(role: "Administrator");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Correct-Horse-99");

        var file = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null }
        });

        var existingAccountId = Guid.NewGuid();
        _accountHolderService
            .Setup(s => s.GetAccountHolderByEmailAsync("jane@example.com"))
            .ReturnsAsync(MakeDto(id: existingAccountId.ToString(), email: "jane@example.com"));
        _accountHolderService
            .Setup(s => s.AddStudentToAccountAsync(existingAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ReturnsAsync(MakeStudentDto);

        var result = await _controller.BulkImportFamilies(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FamilyImportResponse>(ok.Value);
        var family = Assert.Single(response.Families);

        Assert.False(family.ParentAccountCreated);
        Assert.True(family.Success);
        _keycloakService.Verify(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
    }

    [Fact]
    public async Task BulkImportFamilies_ChildFailsForNewParent_RollsBackChildrenAndDeletesNewAccount()
    {
        SetUser(role: "Administrator");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Correct-Horse-99");

        var file = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null },
            new object?[] { "Jane", "Smith", "jane@example.com", "Sam", "Smith", null, null },
        });

        var newAccountId = Guid.NewGuid();
        var firstChildId = Guid.NewGuid();
        _accountHolderService.Setup(s => s.GetAccountHolderByEmailAsync("jane@example.com")).ReturnsAsync((AccountHolderDto?)null);
        _keycloakService
            .Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(new CreateUserResponse { UserId = "kc-jane", Username = "jane@example.com", IsTemporary = true, TemporaryPassword = "Correct-Horse-99" });
        _keycloakService
            .Setup(s => s.GetUserIdByEmailAsync("jane@example.com"))
            .ReturnsAsync("kc-jane");
        _accountHolderService
            .Setup(s => s.CreateAccountHolderAsync(It.IsAny<CreateAccountHolderDto>(), "kc-jane"))
            .ReturnsAsync(MakeDto(id: newAccountId.ToString(), email: "jane@example.com"));
        _accountHolderService
            .SetupSequence(s => s.AddStudentToAccountAsync(newAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ReturnsAsync(() => { var dto = MakeStudentDto(); dto.Id = firstChildId; return dto; })
            .ThrowsAsync(new InvalidOperationException("simulated failure adding second child"));

        var result = await _controller.BulkImportFamilies(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FamilyImportResponse>(ok.Value);
        var family = Assert.Single(response.Families);

        Assert.False(family.Success);
        Assert.Empty(family.Children);
        _accountHolderService.Verify(s => s.RemoveStudentFromAccountAsync(newAccountId, firstChildId), Times.Once);
        _accountHolderService.Verify(s => s.DeleteAccountHolderAsync(newAccountId), Times.Once);
        _keycloakService.Verify(s => s.GetUserIdByEmailAsync("jane@example.com"), Times.Once);
        _keycloakService.Verify(s => s.DeleteUserAsync("kc-jane"), Times.Once);
    }

    [Fact]
    public async Task BulkImportFamilies_ChildFailsForNewParent_KeycloakCleanupFailureDoesNotFailTheRequest()
    {
        SetUser(role: "Administrator");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Correct-Horse-99");

        var file = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null },
            new object?[] { "Jane", "Smith", "jane@example.com", "Sam", "Smith", null, null },
        });

        var newAccountId = Guid.NewGuid();
        _accountHolderService.Setup(s => s.GetAccountHolderByEmailAsync("jane@example.com")).ReturnsAsync((AccountHolderDto?)null);
        _keycloakService
            .Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(new CreateUserResponse { UserId = "kc-jane", Username = "jane@example.com", IsTemporary = true, TemporaryPassword = "Correct-Horse-99" });
        _keycloakService
            .Setup(s => s.GetUserIdByEmailAsync("jane@example.com"))
            .ThrowsAsync(new InvalidOperationException("Keycloak admin API unreachable"));
        _accountHolderService
            .Setup(s => s.CreateAccountHolderAsync(It.IsAny<CreateAccountHolderDto>(), "kc-jane"))
            .ReturnsAsync(MakeDto(id: newAccountId.ToString(), email: "jane@example.com"));
        _accountHolderService
            .SetupSequence(s => s.AddStudentToAccountAsync(newAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ReturnsAsync(MakeStudentDto)
            .ThrowsAsync(new InvalidOperationException("simulated failure adding second child"));

        var result = await _controller.BulkImportFamilies(file, CancellationToken.None);

        // The Keycloak cleanup lookup failing must not blow up the whole request/response.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FamilyImportResponse>(ok.Value);
        var family = Assert.Single(response.Families);
        Assert.False(family.Success);
        _accountHolderService.Verify(s => s.DeleteAccountHolderAsync(newAccountId), Times.Once);
    }

    [Fact]
    public async Task BulkImportFamilies_ReuploadAfterRollback_CreatesNewAccountInstepOfCollidingWithOrphan()
    {
        // Regression test for the retry-after-rollback risk: a first upload rolls back a new
        // parent (deleting the DB row and, now, the Keycloak user too), so a second upload for
        // the same email must be free to create a brand new Keycloak user rather than colliding
        // with an orphaned one from the first attempt.
        SetUser(role: "Administrator");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Correct-Horse-99");

        var firstAttemptFile = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null },
            new object?[] { "Jane", "Smith", "jane@example.com", "Sam", "Smith", null, null },
        });
        var secondAttemptFile = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null },
        });

        var firstAccountId = Guid.NewGuid();
        var firstChildId = Guid.NewGuid();
        _accountHolderService.Setup(s => s.GetAccountHolderByEmailAsync("jane@example.com")).ReturnsAsync((AccountHolderDto?)null);
        _keycloakService
            .Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(new CreateUserResponse { UserId = "kc-jane-1", Username = "jane@example.com", IsTemporary = true, TemporaryPassword = "Correct-Horse-99" });
        _keycloakService
            .Setup(s => s.GetUserIdByEmailAsync("jane@example.com"))
            .ReturnsAsync("kc-jane-1");
        _accountHolderService
            .Setup(s => s.CreateAccountHolderAsync(It.IsAny<CreateAccountHolderDto>(), "kc-jane-1"))
            .ReturnsAsync(MakeDto(id: firstAccountId.ToString(), email: "jane@example.com"));
        _accountHolderService
            .SetupSequence(s => s.AddStudentToAccountAsync(firstAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ReturnsAsync(() => { var dto = MakeStudentDto(); dto.Id = firstChildId; return dto; })
            .ThrowsAsync(new InvalidOperationException("simulated failure adding second child"));

        var firstResult = await _controller.BulkImportFamilies(firstAttemptFile, CancellationToken.None);
        var firstOk = Assert.IsType<OkObjectResult>(firstResult.Result);
        Assert.False(Assert.Single(Assert.IsType<FamilyImportResponse>(firstOk.Value).Families).Success);

        // After rollback, the account holder is gone and (thanks to the new cleanup call) so is
        // the Keycloak user, so the re-upload's "does this email already have an account" lookup
        // must again see nothing, and Keycloak must be free to hand back a fresh user id.
        _accountHolderService.Setup(s => s.GetAccountHolderByEmailAsync("jane@example.com")).ReturnsAsync((AccountHolderDto?)null);
        var secondAccountId = Guid.NewGuid();
        _keycloakService
            .Setup(s => s.CreateUserAsync(It.Is<CreateUserRequest>(r => r.Email == "jane@example.com")))
            .ReturnsAsync(new CreateUserResponse { UserId = "kc-jane-2", Username = "jane@example.com", IsTemporary = true, TemporaryPassword = "Correct-Horse-99" });
        _accountHolderService
            .Setup(s => s.CreateAccountHolderAsync(It.IsAny<CreateAccountHolderDto>(), "kc-jane-2"))
            .ReturnsAsync(MakeDto(id: secondAccountId.ToString(), email: "jane@example.com"));
        _accountHolderService
            .Setup(s => s.AddStudentToAccountAsync(secondAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ReturnsAsync(MakeStudentDto);

        var secondResult = await _controller.BulkImportFamilies(secondAttemptFile, CancellationToken.None);

        var secondOk = Assert.IsType<OkObjectResult>(secondResult.Result);
        var secondFamily = Assert.Single(Assert.IsType<FamilyImportResponse>(secondOk.Value).Families);
        Assert.True(secondFamily.Success);
        Assert.True(secondFamily.ParentAccountCreated);
        _keycloakService.Verify(s => s.DeleteUserAsync("kc-jane-1"), Times.Once);
    }

    [Fact]
    public async Task BulkImportFamilies_ChildFailsForExistingParent_RollsBackChildrenButKeepsAccount()
    {
        SetUser(role: "Administrator");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Correct-Horse-99");

        var file = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null },
            new object?[] { "Jane", "Smith", "jane@example.com", "Sam", "Smith", null, null },
        });

        var existingAccountId = Guid.NewGuid();
        var firstChildId = Guid.NewGuid();
        _accountHolderService
            .Setup(s => s.GetAccountHolderByEmailAsync("jane@example.com"))
            .ReturnsAsync(MakeDto(id: existingAccountId.ToString(), email: "jane@example.com"));
        _accountHolderService
            .SetupSequence(s => s.AddStudentToAccountAsync(existingAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ReturnsAsync(() => { var dto = MakeStudentDto(); dto.Id = firstChildId; return dto; })
            .ThrowsAsync(new InvalidOperationException("simulated failure adding second child"));

        var result = await _controller.BulkImportFamilies(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FamilyImportResponse>(ok.Value);
        var family = Assert.Single(response.Families);

        Assert.False(family.Success);
        _accountHolderService.Verify(s => s.RemoveStudentFromAccountAsync(existingAccountId, firstChildId), Times.Once);
        _accountHolderService.Verify(s => s.DeleteAccountHolderAsync(It.IsAny<Guid>()), Times.Never);
        _keycloakService.Verify(s => s.GetUserIdByEmailAsync(It.IsAny<string>()), Times.Never);
        _keycloakService.Verify(s => s.DeleteUserAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BulkImportFamilies_MultipleFamilies_ReportsIndependentPerFamilyResults()
    {
        SetUser(role: "Administrator");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Correct-Horse-99");

        var file = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null },
            new object?[] { "Bob", "Lee", "bob@example.com", "Cara", "Lee", null, null },
        });

        var janeAccountId = Guid.NewGuid();
        var bobAccountId = Guid.NewGuid();
        _accountHolderService.Setup(s => s.GetAccountHolderByEmailAsync("jane@example.com")).ReturnsAsync((AccountHolderDto?)null);
        _accountHolderService.Setup(s => s.GetAccountHolderByEmailAsync("bob@example.com")).ReturnsAsync((AccountHolderDto?)null);
        _keycloakService
            .Setup(s => s.CreateUserAsync(It.Is<CreateUserRequest>(r => r.Email == "jane@example.com")))
            .ReturnsAsync(new CreateUserResponse { UserId = "kc-jane", Username = "jane@example.com", IsTemporary = true, TemporaryPassword = "Correct-Horse-99" });
        _keycloakService
            .Setup(s => s.CreateUserAsync(It.Is<CreateUserRequest>(r => r.Email == "bob@example.com")))
            .ReturnsAsync(new CreateUserResponse { UserId = "kc-bob", Username = "bob@example.com", IsTemporary = true, TemporaryPassword = "Correct-Horse-99" });
        _accountHolderService
            .Setup(s => s.CreateAccountHolderAsync(It.IsAny<CreateAccountHolderDto>(), "kc-jane"))
            .ReturnsAsync(MakeDto(id: janeAccountId.ToString(), email: "jane@example.com"));
        _accountHolderService
            .Setup(s => s.CreateAccountHolderAsync(It.IsAny<CreateAccountHolderDto>(), "kc-bob"))
            .ReturnsAsync(MakeDto(id: bobAccountId.ToString(), email: "bob@example.com"));
        _accountHolderService
            .Setup(s => s.AddStudentToAccountAsync(janeAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ReturnsAsync(MakeStudentDto);
        _accountHolderService
            .Setup(s => s.AddStudentToAccountAsync(bobAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ThrowsAsync(new InvalidOperationException("simulated failure"));

        var result = await _controller.BulkImportFamilies(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FamilyImportResponse>(ok.Value);

        Assert.Equal(2, response.Families.Count);
        Assert.Equal(1, response.SuccessCount);
        Assert.Equal(1, response.FailureCount);
        Assert.True(response.Families.Single(f => f.ParentEmail == "jane@example.com").Success);
        Assert.False(response.Families.Single(f => f.ParentEmail == "bob@example.com").Success);
        _accountHolderService.Verify(s => s.DeleteAccountHolderAsync(bobAccountId), Times.Once);
    }

    [Fact]
    public async Task BulkImportFamilies_RowWithMissingRequiredField_ReportsParseErrorAndSkipsRow()
    {
        SetUser(role: "Administrator");
        _tenantSettingsService
            .Setup(s => s.GetValidatedInitialInvitePasswordAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Correct-Horse-99");

        var file = MakeXlsxFile(new object?[][]
        {
            new object?[] { "Jane", "Smith", "jane@example.com", "Alex", "Smith", null, null },
            new object?[] { "Bob", "", "bob@example.com", "Cara", "Lee", null, null },
        });

        var janeAccountId = Guid.NewGuid();
        _accountHolderService.Setup(s => s.GetAccountHolderByEmailAsync("jane@example.com")).ReturnsAsync((AccountHolderDto?)null);
        _keycloakService
            .Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(new CreateUserResponse { UserId = "kc-jane", Username = "jane@example.com", IsTemporary = true, TemporaryPassword = "Correct-Horse-99" });
        _accountHolderService
            .Setup(s => s.CreateAccountHolderAsync(It.IsAny<CreateAccountHolderDto>(), "kc-jane"))
            .ReturnsAsync(MakeDto(id: janeAccountId.ToString(), email: "jane@example.com"));
        _accountHolderService
            .Setup(s => s.AddStudentToAccountAsync(janeAccountId, It.IsAny<CreateStudentForAccountDto>()))
            .ReturnsAsync(MakeStudentDto);

        var result = await _controller.BulkImportFamilies(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FamilyImportResponse>(ok.Value);

        Assert.Equal(1, response.TotalFamilies);
        var parseError = Assert.Single(response.ParseErrors);
        Assert.Contains("ParentLastName", parseError.Message);
    }

    private static IFormFile MakeXlsxFile(object?[][] dataRows, string[]? headers = null, string fileName = "families.xlsx")
    {
        headers ??= FamilyImportTemplateGenerator.Headers;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Family Import");
        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
        }

        for (var rowIndex = 0; rowIndex < dataRows.Length; rowIndex++)
        {
            var row = dataRows[rowIndex];
            for (var col = 0; col < row.Length; col++)
            {
                var cell = sheet.Cell(rowIndex + 2, col + 1);
                switch (row[col])
                {
                    case null:
                        break;
                    case DateTime dt:
                        cell.Value = dt;
                        break;
                    default:
                        cell.Value = row[col]!.ToString();
                        break;
                }
            }
        }

        using var workbookStream = new MemoryStream();
        workbook.SaveAs(workbookStream);
        var bytes = workbookStream.ToArray();
        var formStream = new MemoryStream(bytes);
        return new FormFile(formStream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    // -------------------------------------------------------------------------
    // GET /api/accountholders/bulk-import/template (admin)
    // -------------------------------------------------------------------------

    [Fact]
    public void DownloadFamilyImportTemplate_ReturnsXlsxFile()
    {
        SetUser(role: "Administrator");

        var result = _controller.DownloadFamilyImportTemplate();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.Equal("family-import-template.xlsx", fileResult.FileDownloadName);
        Assert.NotEmpty(fileResult.FileContents);
    }

    // -------------------------------------------------------------------------
    // PUT /api/accountholders/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAccountHolder_WhenOwnerAndFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var keycloakId = "kc-owner";
        var myDto = MakeDto(id: id.ToString());
        SetUser(keycloakId: keycloakId);
        // CanUserUpdateAccountHolder: not admin, look up by keycloak ID → returns the same account
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync(keycloakId)).ReturnsAsync(myDto);
        _accountHolderService.Setup(s => s.UpdateAccountHolderAsync(id, It.IsAny<UpdateAccountHolderDto>())).ReturnsAsync(myDto);

        var result = await _controller.UpdateAccountHolder(id, new UpdateAccountHolderDto { FirstName = "Updated" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(myDto, ok.Value);
    }

    [Fact]
    public async Task UpdateAccountHolder_WhenOwnerAndNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var keycloakId = "kc-owner";
        var myDto = MakeDto(id: id.ToString());
        SetUser(keycloakId: keycloakId);
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync(keycloakId)).ReturnsAsync(myDto);
        _accountHolderService.Setup(s => s.UpdateAccountHolderAsync(id, It.IsAny<UpdateAccountHolderDto>())).ReturnsAsync((AccountHolderDto?)null);

        var result = await _controller.UpdateAccountHolder(id, new UpdateAccountHolderDto());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAccountHolder_WhenNotOwnerAndNotAdmin_ReturnsForbid()
    {
        var id = Guid.NewGuid();
        SetUser(keycloakId: "kc-other");
        // GetAccountHolderByUserIdAsync returns an account holder with a different ID
        var othersDto = MakeDto(id: Guid.NewGuid().ToString());
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync("kc-other")).ReturnsAsync(othersDto);

        var result = await _controller.UpdateAccountHolder(id, new UpdateAccountHolderDto());

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAccountHolder_WhenLinkedEmailChangeRejected_ReturnsBadRequest()
    {
        var id = Guid.NewGuid();
        var keycloakId = "kc-owner";
        var myDto = MakeDto(id: id.ToString());
        SetUser(keycloakId: keycloakId);
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync(keycloakId)).ReturnsAsync(myDto);
        _accountHolderService
            .Setup(s => s.UpdateAccountHolderAsync(id, It.IsAny<UpdateAccountHolderDto>()))
            .ThrowsAsync(new InvalidOperationException("Email changes for linked members must be confirmed through profile settings."));

        var result = await _controller.UpdateAccountHolder(id, new UpdateAccountHolderDto { EmailAddress = "updated@example.com" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Email changes for linked members must be confirmed through profile settings.", badRequest.Value);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/accountholders/me/students/{studentId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveStudentFromMyAccount_WhenSuccess_ReturnsNoContent()
    {
        var keycloakId = "kc-user-1";
        var studentId = Guid.NewGuid();
        var accountHolderId = Guid.NewGuid();
        var myDto = MakeDto(id: accountHolderId.ToString());
        SetUser(keycloakId: keycloakId);
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync(keycloakId)).ReturnsAsync(myDto);
        _accountHolderService.Setup(s => s.RemoveStudentFromAccountAsync(accountHolderId, studentId)).ReturnsAsync(true);

        var result = await _controller.RemoveStudentFromMyAccount(studentId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveStudentFromMyAccount_WhenNotFound_ReturnsNotFound()
    {
        var keycloakId = "kc-user-1";
        var studentId = Guid.NewGuid();
        var accountHolderId = Guid.NewGuid();
        var myDto = MakeDto(id: accountHolderId.ToString());
        SetUser(keycloakId: keycloakId);
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync(keycloakId)).ReturnsAsync(myDto);
        _accountHolderService.Setup(s => s.RemoveStudentFromAccountAsync(accountHolderId, studentId)).ReturnsAsync(false);

        var result = await _controller.RemoveStudentFromMyAccount(studentId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/accountholders/{accountHolderId}/students/{studentId} (admin)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveStudentFromAccount_WhenSuccess_ReturnsNoContent()
    {
        SetUser(role: "Administrator");
        var accountHolderId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        _accountHolderService.Setup(s => s.RemoveStudentFromAccountAsync(accountHolderId, studentId)).ReturnsAsync(true);

        var result = await _controller.RemoveStudentFromAccount(accountHolderId, studentId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveStudentFromAccount_WhenNotFound_ReturnsNotFound()
    {
        SetUser(role: "Administrator");
        var accountHolderId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        _accountHolderService.Setup(s => s.RemoveStudentFromAccountAsync(accountHolderId, studentId)).ReturnsAsync(false);

        var result = await _controller.RemoveStudentFromAccount(accountHolderId, studentId);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
