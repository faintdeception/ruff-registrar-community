using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.DTOs;
using System.Security.Claims;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountHoldersController : ControllerBase
{
    private readonly IAccountHolderService _accountHolderService;
    private readonly IKeycloakService _keycloakService;
    private readonly ITenantSettingsService _tenantSettingsService;
    private readonly ILogger<AccountHoldersController> _logger;

    public AccountHoldersController(
        IAccountHolderService accountHolderService,
        IKeycloakService keycloakService,
        ITenantSettingsService tenantSettingsService,
        ILogger<AccountHoldersController> logger)
    {
        _accountHolderService = accountHolderService;
        _keycloakService = keycloakService;
        _tenantSettingsService = tenantSettingsService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current user's account holder information
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<AccountHolderDto>> GetMyAccountHolder()
    {
        var keycloakUserId = GetCurrentKeycloakId();
        if (string.IsNullOrEmpty(keycloakUserId))
        {
            return Unauthorized("User ID not found in token");
        }

        var accountHolder = await _accountHolderService.GetAccountHolderByUserIdAsync(keycloakUserId);
        if (accountHolder == null)
        {
            var email = GetCurrentUserEmail();
            if (!string.IsNullOrEmpty(email))
            {
                accountHolder = await _accountHolderService.LinkAccountHolderToUserAsync(email, keycloakUserId);
                if (accountHolder != null)
                {
                    _logger.LogInformation("Linked existing account holder for email {Email} to user {UserId}", email, keycloakUserId);
                }
            }

            if (accountHolder != null)
            {
                return Ok(accountHolder);
            }

            // Auto-create account holder from JWT token claims
            var firstName = GetCurrentUserFirstName();
            var lastName = GetCurrentUserLastName();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                return BadRequest("Insufficient user information in token to create account holder");
            }

            var createDto = new CreateAccountHolderDto
            {
                FirstName = firstName,
                LastName = lastName,
                EmailAddress = email,
                // Set default values for required fields
                AddressJson = new AddressInfo
                {
                    Street = "",
                    City = "",
                    State = "",
                    PostalCode = "",
                    Country = "US"
                },
                EmergencyContactJson = new EmergencyContactInfo
                {
                    FirstName = "",
                    LastName = "",
                    Email = ""
                }
            };

            try
            {
                accountHolder = await _accountHolderService.CreateAccountHolderAsync(createDto, keycloakUserId);
                _logger.LogInformation("Auto-created account holder for user {UserId}", keycloakUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-creating account holder for user {UserId}", keycloakUserId);
                return StatusCode(500, "Error creating account holder");
            }
        }

        return Ok(accountHolder);
    }

    /// <summary>
    /// Get all account holders (admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<IEnumerable<AccountHolderDto>>> GetAllAccountHolders()
    {
        var accountHolders = await _accountHolderService.GetAllAccountHoldersAsync();
        return Ok(accountHolders);
    }

    /// <summary>
    /// Get account holder by ID (admin only)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<AccountHolderDto>> GetAccountHolder(Guid id)
    {
        var accountHolder = await _accountHolderService.GetAccountHolderByIdAsync(id);
        if (accountHolder == null)
        {
            return NotFound();
        }

        return Ok(accountHolder);
    }

    /// <summary>
    /// Create a new account holder (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<CreateAccountHolderResponse>> CreateAccountHolder(CreateAccountHolderDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("=== MEMBER CREATION START ===");
            _logger.LogInformation("Creating new account holder for email: {Email}", createDto.EmailAddress);
            _logger.LogInformation("CreateDto details: {@CreateDto}", createDto);
            
            // Security audit log - Member creation initiated
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["EventType"] = "MemberCreationInitiated",
                ["AdminEmail"] = GetCurrentUserEmail(),
                ["TargetMemberEmail"] = createDto.EmailAddress,
                ["Timestamp"] = DateTime.UtcNow,
                ["IPAddress"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            });
            
            var response = await CreateAccountHolderWithKeycloakUserAsync(createDto, explicitPassword: null, requireEmailVerification: true);

            // Security audit log - Member creation completed successfully
            _logger.LogInformation("Member creation completed successfully. " +
                "AdminEmail: {AdminEmail}, MemberEmail: {MemberEmail}, AccountHolderId: {AccountHolderId}, " +
                "HasTemporaryPassword: {HasTemporaryPassword}, Timestamp: {Timestamp}",
                GetCurrentUserEmail(),
                createDto.EmailAddress,
                response.AccountHolder.Id,
                response.Credentials != null,
                DateTime.UtcNow);
            
            return CreatedAtAction(nameof(GetAccountHolder), new { id = response.AccountHolder.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError("=== MEMBER CREATION FAILED ===");
            _logger.LogError(ex, "Full exception details: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerExceptionType}: {InnerMessage}", 
                    ex.InnerException.GetType().Name, ex.InnerException.Message);
            }
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            
            // Security audit log - Member creation failed
            _logger.LogError(ex, "Member creation failed. " +
                "AdminEmail: {AdminEmail}, TargetMemberEmail: {MemberEmail}, " +
                "ErrorMessage: {ErrorMessage}, Timestamp: {Timestamp}",
                GetCurrentUserEmail(),
                createDto?.EmailAddress ?? "unknown",
                ex.Message,
                DateTime.UtcNow);
                
            return StatusCode(500, "An error occurred while creating the account holder");
        }
    }

    /// <summary>
    /// Bulk import families from an .xlsx file (see the template downloaded from
    /// GET bulk-import/template). Admin only. Each row is a child; rows sharing a ParentEmail
    /// become one family. An existing account for that email gets the children added to it
    /// instead of failing as a duplicate. Each family's parent + children succeed or fail
    /// together (whole-family atomicity) — other families in the same file are unaffected.
    /// No email is sent to new parent accounts; they must change their password on first login.
    /// </summary>
    [HttpPost("bulk-import")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<FamilyImportResponse>> BulkImportFamilies(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("A non-empty .xlsx file is required.");
        }

        string initialInvitePassword;
        try
        {
            // Fail fast, before touching Keycloak for family 1, if the host hasn't configured a
            // secure initial invite password.
            initialInvitePassword = await _tenantSettingsService.GetValidatedInitialInvitePasswordAsync(cancellationToken);
        }
        catch (InsecureInitialInvitePasswordException ex)
        {
            return BadRequest(new { message = ex.Message, reasons = ex.FailureReasons });
        }

        FamilyImportParseResult parseResult;
        try
        {
            await using var stream = file.OpenReadStream();
            parseResult = FamilyImportExcelParser.Parse(stream);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        var response = new FamilyImportResponse
        {
            TotalFamilies = parseResult.Families.Count,
            TotalChildren = parseResult.Families.Sum(f => f.Children.Count),
            ParseErrors = parseResult.RowErrors
                .Select(e => new FamilyImportParseError { RowNumber = e.RowNumber, Message = e.Message })
                .ToList()
        };

        foreach (var family in parseResult.Families)
        {
            response.Families.Add(await ImportFamilyAsync(family, initialInvitePassword));
        }

        response.SuccessCount = response.Families.Count(f => f.Success);
        response.FailureCount = response.Families.Count(f => !f.Success);

        _logger.LogInformation(
            "Bulk family import completed. AdminEmail: {AdminEmail}, TotalFamilies: {TotalFamilies}, " +
            "SuccessCount: {SuccessCount}, FailureCount: {FailureCount}",
            GetCurrentUserEmail(), response.TotalFamilies, response.SuccessCount, response.FailureCount);

        return Ok(response);
    }

    /// <summary>
    /// Creates (or reuses) the family's parent account and adds all of its children. If any
    /// child fails partway through, the children already added for this family are removed so
    /// the family fails or succeeds as a unit; other families in the same upload are unaffected.
    /// Note: if the parent account itself was newly created here, a rollback removes the
    /// AccountHolder row, but the Keycloak user is not deleted (no delete-user capability exists
    /// today) — matches the pre-existing single-create path's known limitation on this edge case.
    /// </summary>
    private async Task<FamilyImportFamilyResult> ImportFamilyAsync(FamilyImportFamily family, string initialInvitePassword)
    {
        var familyResult = new FamilyImportFamilyResult { ParentEmail = family.ParentEmail };

        Guid accountHolderId;
        var parentAccountCreated = false;
        try
        {
            var existingAccountHolder = await _accountHolderService.GetAccountHolderByEmailAsync(family.ParentEmail);
            if (existingAccountHolder != null)
            {
                accountHolderId = Guid.Parse(existingAccountHolder.Id);
            }
            else
            {
                var createDto = new CreateAccountHolderDto
                {
                    FirstName = family.ParentFirstName,
                    LastName = family.ParentLastName,
                    EmailAddress = family.ParentEmail
                };

                var created = await CreateAccountHolderWithKeycloakUserAsync(
                    createDto,
                    explicitPassword: initialInvitePassword,
                    requireEmailVerification: false);

                accountHolderId = Guid.Parse(created.AccountHolder.Id);
                parentAccountCreated = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk family import failed to create/find parent account for {ParentEmail}", family.ParentEmail);
            familyResult.Success = false;
            familyResult.ErrorMessage = ex.Message;
            return familyResult;
        }

        familyResult.ParentAccountCreated = parentAccountCreated;

        var addedStudentIds = new List<Guid>();
        try
        {
            foreach (var child in family.Children)
            {
                var studentDto = await _accountHolderService.AddStudentToAccountAsync(accountHolderId, new CreateStudentForAccountDto
                {
                    FirstName = child.ChildFirstName,
                    LastName = child.ChildLastName,
                    Grade = child.ChildGrade,
                    DateOfBirth = child.ChildDateOfBirth
                });

                addedStudentIds.Add(studentDto.Id);
                familyResult.Children.Add(new FamilyImportChildResult
                {
                    ChildFirstName = child.ChildFirstName,
                    ChildLastName = child.ChildLastName,
                    Success = true
                });
            }

            familyResult.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk family import failed adding a child for {ParentEmail}; rolling back this family's children", family.ParentEmail);

            foreach (var studentId in addedStudentIds)
            {
                await _accountHolderService.RemoveStudentFromAccountAsync(accountHolderId, studentId);
            }

            if (parentAccountCreated)
            {
                await _accountHolderService.DeleteAccountHolderAsync(accountHolderId);
                await DeleteOrphanedKeycloakUserAsync(family.ParentEmail);
            }

            familyResult.Children.Clear();
            familyResult.Success = false;
            familyResult.ErrorMessage = ex.Message;
        }

        return familyResult;
    }

    /// <summary>
    /// Best-effort cleanup for a rolled-back new-parent family: deletes the Keycloak user created
    /// moments earlier for this email so a later re-upload of the same family doesn't collide with
    /// an orphaned Keycloak account. Failures here are logged, not thrown — the family import
    /// itself has already failed and been reported; a cleanup miss just means the admin needs to
    /// handle that one Keycloak user manually before retrying.
    /// </summary>
    private async Task DeleteOrphanedKeycloakUserAsync(string parentEmail)
    {
        try
        {
            var keycloakUserId = await _keycloakService.GetUserIdByEmailAsync(parentEmail);
            if (!string.IsNullOrEmpty(keycloakUserId))
            {
                await _keycloakService.DeleteUserAsync(keycloakUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up orphaned Keycloak user for {ParentEmail} after a rolled-back bulk import", parentEmail);
        }
    }

    /// <summary>
    /// Downloads the .xlsx template for the family bulk-import flow (parent + children per
    /// family, one row per child). Admin only.
    /// </summary>
    [HttpGet("bulk-import/template")]
    [Authorize(Roles = "Administrator")]
    public IActionResult DownloadFamilyImportTemplate()
    {
        var bytes = FamilyImportTemplateGenerator.Generate();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "family-import-template.xlsx");
    }

    /// <summary>
    /// Creates a Keycloak user and the corresponding AccountHolder in sequence. Shared by the
    /// single-create endpoint and bulk import so there's exactly one place that does this.
    /// </summary>
    private async Task<CreateAccountHolderResponse> CreateAccountHolderWithKeycloakUserAsync(
        CreateAccountHolderDto createDto,
        string? explicitPassword,
        bool requireEmailVerification)
    {
        var createUserRequest = new CreateUserRequest
        {
            Email = createDto.EmailAddress,
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Role = StudentRegistrar.Models.UserRole.Member, // Default role for members
            Password = explicitPassword ?? "", // Blank => Keycloak service generates a random one
            RequireEmailVerification = requireEmailVerification
        };

        var keycloakUserResponse = await _keycloakService.CreateUserAsync(createUserRequest);
        var accountHolder = await _accountHolderService.CreateAccountHolderAsync(createDto, keycloakUserResponse.UserId);

        return new CreateAccountHolderResponse
        {
            AccountHolder = accountHolder,
            Credentials = keycloakUserResponse.IsTemporary ? new UserCredentials
            {
                Username = keycloakUserResponse.Username,
                TemporaryPassword = keycloakUserResponse.TemporaryPassword ?? "",
                MustChangePassword = true
            } : null,
            Message = keycloakUserResponse.IsTemporary
                ? "User must change password on first login"
                : "Account created successfully"
        };
    }

    /// <summary>
    /// Update account holder information
    /// Users can update their own profile, admins can update any profile
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountHolderDto>> UpdateAccountHolder(Guid id, UpdateAccountHolderDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if user can update this account holder
        var canUpdate = await CanUserUpdateAccountHolder(id);
        if (!canUpdate)
        {
            return Forbid("You can only update your own account");
        }

        try
        {
            var updatedAccountHolder = await _accountHolderService.UpdateAccountHolderAsync(id, updateDto);
            if (updatedAccountHolder == null)
            {
                return NotFound();
            }

            return Ok(updatedAccountHolder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid account holder update for {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account holder {Id}", id);
            return StatusCode(500, "An error occurred while updating the account holder");
        }
    }

    /// <summary>
    /// Add a student to the current user's account
    /// </summary>
    [HttpPost("me/students")]
    public async Task<ActionResult<StudentDto>> AddStudentToMyAccount(CreateStudentForAccountDto createStudentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var keycloakUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(keycloakUserId))
        {
            return Unauthorized("User ID not found in token");
        }

        // Get the account holder to find their ID
        var accountHolder = await _accountHolderService.GetAccountHolderByUserIdAsync(keycloakUserId);
        if (accountHolder == null)
        {
            return NotFound("Account holder not found");
        }

        if (!Guid.TryParse(accountHolder.Id, out var accountHolderId))
        {
            return BadRequest("Invalid account holder ID");
        }

        try
        {
            var student = await _accountHolderService.AddStudentToAccountAsync(accountHolderId, createStudentDto);
            return CreatedAtAction("GetStudent", "Students", new { id = student.Id }, student);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding student to account holder {AccountHolderId}", accountHolderId);
            return StatusCode(500, "An error occurred while adding the student");
        }
    }

    /// <summary>
    /// Add a student to a specific account holder (admin only)
    /// </summary>
    [HttpPost("{accountHolderId:guid}/students")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<StudentDto>> AddStudentToAccount(Guid accountHolderId, CreateStudentForAccountDto createStudentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var student = await _accountHolderService.AddStudentToAccountAsync(accountHolderId, createStudentDto);
            return CreatedAtAction("GetStudent", "Students", new { id = student.Id }, student);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding student to account holder {AccountHolderId}", accountHolderId);
            return StatusCode(500, "An error occurred while adding the student");
        }
    }

    /// <summary>
    /// Remove a student from the current user's account
    /// </summary>
    [HttpDelete("me/students/{studentId:guid}")]
    public async Task<IActionResult> RemoveStudentFromMyAccount(Guid studentId)
    {
        var keycloakUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(keycloakUserId))
        {
            return Unauthorized("User ID not found in token");
        }

        // Get the account holder to find their ID
        var accountHolder = await _accountHolderService.GetAccountHolderByUserIdAsync(keycloakUserId);
        if (accountHolder == null)
        {
            return NotFound("Account holder not found");
        }

        if (!Guid.TryParse(accountHolder.Id, out var accountHolderId))
        {
            return BadRequest("Invalid account holder ID");
        }

        var success = await _accountHolderService.RemoveStudentFromAccountAsync(accountHolderId, studentId);
        if (!success)
        {
            return NotFound("Student not found or does not belong to this account");
        }

        return NoContent();
    }

    /// <summary>
    /// Remove a student from a specific account holder (admin only)
    /// </summary>
    [HttpDelete("{accountHolderId:guid}/students/{studentId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> RemoveStudentFromAccount(Guid accountHolderId, Guid studentId)
    {
        var success = await _accountHolderService.RemoveStudentFromAccountAsync(accountHolderId, studentId);
        if (!success)
        {
            return NotFound("Student not found or does not belong to this account");
        }

        return NoContent();
    }

    private async Task<bool> CanUserUpdateAccountHolder(Guid accountHolderId)
    {
        // Admins can update any account holder
        if (User.IsInRole("Administrator"))
        {
            return true;
        }

        // Users can only update their own account
        var keycloakUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(keycloakUserId))
        {
            return false;
        }

        var currentUserAccountHolder = await _accountHolderService.GetAccountHolderByUserIdAsync(keycloakUserId);
        if (currentUserAccountHolder == null)
        {
            return false;
        }

        return Guid.TryParse(currentUserAccountHolder.Id, out var currentAccountHolderId) && 
               currentAccountHolderId == accountHolderId;
    }

    private string GetCurrentKeycloakId()
    {
        // Try multiple claim types that might contain the subject ID
        var subClaim = User.FindFirst("sub")?.Value 
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        
        if (string.IsNullOrEmpty(subClaim))
        {
            throw new UnauthorizedAccessException("No user ID in token");
        }
        return subClaim;
    }

    private string GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? "";
    }

    private string GetCurrentUserFirstName()
    {
        return User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.FindFirst("given_name")?.Value ?? "";
    }

    private string GetCurrentUserLastName()
    {
        return User.FindFirst(ClaimTypes.Surname)?.Value ?? User.FindFirst("family_name")?.Value ?? "";
    }
}
