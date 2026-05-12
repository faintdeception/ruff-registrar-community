using System.Security.Claims;
using FluentAssertions;
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
    private readonly AccountHoldersController _controller;

    public AccountHoldersControllerTests()
    {
        _controller = new AccountHoldersController(
            _accountHolderService.Object,
            _keycloakService.Object,
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

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(dtos);
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

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetAccountHolder_WhenNotFound_ReturnsNotFound()
    {
        SetUser(role: "Administrator");
        _accountHolderService.Setup(s => s.GetAccountHolderByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AccountHolderDto?)null);

        var result = await _controller.GetAccountHolder(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
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

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetMyAccountHolder_WhenNotFoundButEmailLinks_ReturnsOk()
    {
        SetUser(keycloakId: "kc-user-2", email: "linked@example.com");
        var dto = MakeDto(email: "linked@example.com");
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync("kc-user-2")).ReturnsAsync((AccountHolderDto?)null);
        _accountHolderService.Setup(s => s.LinkAccountHolderToUserAsync("linked@example.com", "kc-user-2")).ReturnsAsync(dto);

        var result = await _controller.GetMyAccountHolder();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(dto);
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

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.ActionName.Should().Be(nameof(_controller.GetAccountHolder));
        var response = created.Value.Should().BeOfType<CreateAccountHolderResponse>().Subject;
        response.AccountHolder.Should().BeEquivalentTo(createdDto);
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

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(myDto);
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

        result.Result.Should().BeOfType<NotFoundResult>();
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

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // -------------------------------------------------------------------------
    // POST /api/accountholders/me/students
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddStudentToMyAccount_ForwardsStudentInfoAndNotes()
    {
        var keycloakId = "kc-user-1";
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
                PreferredName = "Ave",
                ParentNotes = "Call before administering medication."
            }
        };
        var accountHolder = MakeDto(id: accountHolderId.ToString());
        var createdStudent = MakeStudentDto();

        SetUser(keycloakId: keycloakId);
        _accountHolderService.Setup(s => s.GetAccountHolderByUserIdAsync(keycloakId)).ReturnsAsync(accountHolder);
        _accountHolderService.Setup(s => s.AddStudentToAccountAsync(accountHolderId, createDto)).ReturnsAsync(createdStudent);

        var result = await _controller.AddStudentToMyAccount(createDto);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be("GetStudent");
        created.RouteValues!["id"].Should().Be(createdStudent.Id);
        created.Value.Should().BeEquivalentTo(createdStudent);

        _accountHolderService.Verify(s => s.AddStudentToAccountAsync(accountHolderId, It.Is<CreateStudentForAccountDto>(dto =>
            dto.Notes == createDto.Notes &&
            dto.StudentInfoJson != null &&
            dto.StudentInfoJson.SpecialConditions.SequenceEqual(new[] { "ADHD" }) &&
            dto.StudentInfoJson.Allergies.SequenceEqual(new[] { "Peanuts" }) &&
            dto.StudentInfoJson.PreferredName == "Ave" &&
            dto.StudentInfoJson.ParentNotes == "Call before administering medication.")), Times.Once);
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

        result.Should().BeOfType<NoContentResult>();
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

        result.Should().BeOfType<NotFoundObjectResult>();
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

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveStudentFromAccount_WhenNotFound_ReturnsNotFound()
    {
        SetUser(role: "Administrator");
        var accountHolderId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        _accountHolderService.Setup(s => s.RemoveStudentFromAccountAsync(accountHolderId, studentId)).ReturnsAsync(false);

        var result = await _controller.RemoveStudentFromAccount(accountHolderId, studentId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
