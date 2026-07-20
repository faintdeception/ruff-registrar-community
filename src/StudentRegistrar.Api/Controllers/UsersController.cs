using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly StudentRegistrarDbContext _context;
    private readonly IMapper _mapper;
    private readonly IKeycloakService _keycloakService;
    private readonly IUserIdentityEmailSender _userIdentityEmailSender;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        StudentRegistrarDbContext context, 
        IMapper mapper, 
        IKeycloakService keycloakService,
        IUserIdentityEmailSender userIdentityEmailSender,
        ITenantContextAccessor tenantContextAccessor,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<UsersController> logger)
    {
        _context = context;
        _mapper = mapper;
        _keycloakService = keycloakService;
        _userIdentityEmailSender = userIdentityEmailSender;
        _tenantContextAccessor = tenantContextAccessor;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,Educator")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users
            .Include(u => u.UserProfile)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();
        
        // Users can only view their own profile unless they're admin/educator
        if (currentUserId != id && currentUserRole != UserRole.Administrator && currentUserRole != UserRole.Educator)
        {
            return Forbid();
        }

        var user = await _context.Users
            .Include(u => u.UserProfile)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        try
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("User with this email already exists");
            }

            // Create user in Keycloak first
            var keycloakUserResponse = await _keycloakService.CreateUserAsync(request);
            
            // Create user in our database
            var user = _mapper.Map<User>(request);
            user.KeycloakId = keycloakUserResponse.UserId;
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create user profile if provided
            if (request.Profile != null)
            {
                var profile = _mapper.Map<UserProfile>(request.Profile);
                profile.UserId = user.Id;
                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            // Reload user with profile
            var createdUser = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, _mapper.Map<UserDto>(createdUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "Error creating user");
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
    {
        var currentUserRole = GetCurrentUserRole();

        var user = await _context.Users
            .Include(u => u.UserProfile)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        // Users can only update their own profile unless they're admin.
        if (currentUserRole != UserRole.Administrator && !string.Equals(user.KeycloakId, GetCurrentKeycloakId(), StringComparison.Ordinal))
        {
            return Forbid();
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Email changes must be confirmed through the email change request flow.");
            }

            // Update user properties
            _mapper.Map(request, user);
            user.UpdatedAt = DateTime.UtcNow;

            // Update profile if provided
            if (request.Profile != null)
            {
                if (user.UserProfile == null)
                {
                    user.UserProfile = new UserProfile { UserId = user.Id };
                    _context.UserProfiles.Add(user.UserProfile);
                }
                _mapper.Map(request.Profile, user.UserProfile);
            }

            // Update in Keycloak if role changed (admin only)
            if (request.Role.HasValue && currentUserRole == UserRole.Administrator)
            {
                await _keycloakService.UpdateUserRoleAsync(user.KeycloakId, request.Role.Value);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating user {UserId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, "Error updating user");
        }
    }

    [HttpPost("{id}/email-change-requests")]
    [Authorize]
    public async Task<ActionResult<RequestEmailChangeResponse>> RequestEmailChange(Guid id, RequestEmailChangeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserRole = GetCurrentUserRole();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        if (currentUserRole != UserRole.Administrator && !string.Equals(user.KeycloakId, GetCurrentKeycloakId(), StringComparison.Ordinal))
        {
            return Forbid();
        }

        try
        {
            var normalizedEmail = NormalizeEmail(request.NewEmail);
            if (normalizedEmail == null)
            {
                return BadRequest("Email cannot be empty.");
            }

            if (string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var hadPendingEmail = !string.IsNullOrWhiteSpace(user.PendingEmail);
                ClearPendingEmailChange(user);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new RequestEmailChangeResponse
                {
                    CurrentEmail = user.Email,
                    PendingEmail = null,
                    PendingEmailExpiresAtUtc = null,
                    Message = hadPendingEmail
                        ? "Pending email change cancelled. Your current email remains active."
                        : "That email is already active on your account."
                });
            }

            await EnsureEmailIsAvailableAsync(user, normalizedEmail);

            var token = GenerateEmailChangeToken();
            var now = DateTime.UtcNow;
            var expiresAtUtc = now.AddHours(24);

            user.PendingEmail = normalizedEmail;
            user.PendingEmailTokenHash = HashEmailChangeToken(token);
            user.PendingEmailRequestedAtUtc = now;
            user.PendingEmailExpiresAtUtc = expiresAtUtc;
            user.UpdatedAt = now;

            await _context.SaveChangesAsync();

            var confirmationUrl = BuildEmailChangeConfirmationUrl(token);
            var dispatchResult = await _userIdentityEmailSender.SendEmailChangeConfirmationAsync(
                new PendingEmailChangeEmail(user.Email, normalizedEmail, confirmationUrl, expiresAtUtc));

            return Ok(new RequestEmailChangeResponse
            {
                CurrentEmail = user.Email,
                PendingEmail = user.PendingEmail,
                PendingEmailExpiresAtUtc = user.PendingEmailExpiresAtUtc,
                Message = "Confirmation email sent. Your current sign-in email remains active until you verify the new address.",
                DebugConfirmationUrl = _environment.IsDevelopment() ? dispatchResult.DebugConfirmationUrl : null
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Unable to request email change for user {UserId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting email change for user {UserId}", id);
            return StatusCode(500, "Error requesting email change");
        }
    }

    [HttpPost("confirm-email-change")]
    [AllowAnonymous]
    public async Task<ActionResult<ConfirmEmailChangeResponse>> ConfirmEmailChange(ConfirmEmailChangeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var token = request.Token?.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Email change token is required.");
        }

        var tokenHash = HashEmailChangeToken(token);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PendingEmailTokenHash == tokenHash);
        if (user == null || string.IsNullOrWhiteSpace(user.PendingEmail) || user.PendingEmailExpiresAtUtc == null || user.PendingEmailExpiresAtUtc <= DateTime.UtcNow)
        {
            return BadRequest("Email change link is invalid or expired.");
        }

        try
        {
            await EnsureEmailIsAvailableAsync(user, user.PendingEmail);

            await _keycloakService.UpdateUserEmailAsync(user.KeycloakId, user.PendingEmail);
            user.Email = user.PendingEmail;
            user.UpdatedAt = DateTime.UtcNow;

            await SyncLinkedIdentityEmailAsync(user.KeycloakId, user.PendingEmail);
            ClearPendingEmailChange(user);

            await _context.SaveChangesAsync();

            return Ok(new ConfirmEmailChangeResponse
            {
                Email = user.Email,
                Message = "Email address confirmed. Use the new email address the next time you sign in."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Unable to confirm email change for user {UserId}", user.Id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email change for user {UserId}", user.Id);
            return StatusCode(500, "Error confirming email change");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // Deactivate in Keycloak
            await _keycloakService.DeactivateUserAsync(user.KeycloakId);
            
            // Mark as inactive in our database
            user.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, "Error deleting user");
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var keycloakId = GetCurrentKeycloakId();
            
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (user == null)
            {
                // Check if this is a service account token
                var username = User.FindFirst("preferred_username")?.Value;
                if (username != null && username.StartsWith("service-account-"))
                {
                    // Return a mock user for service account
                    return Ok(new UserDto
                    {
                        Id = Guid.Empty,
                        Username = username,
                        Email = "service@example.com",
                        FirstName = "Service",
                        LastName = "Account",
                        KeycloakId = keycloakId,
                        Roles = new List<string> { "Administrator" }
                    });
                }
                
                return NotFound("User not found in system");
            }

            // Map the user entity to DTO
            var userDto = _mapper.Map<UserDto>(user);
            
            // Populate additional properties from JWT token claims
            userDto.Username = User.FindFirst("preferred_username")?.Value ?? string.Empty;
            userDto.KeycloakId = keycloakId;
            userDto.Roles = GetCurrentUserRoles().ToList();

            return Ok(userDto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid token");
        }
    }

    [HttpPost("me/sync")]
    [Authorize]
    public async Task<ActionResult<UserDto>> SyncCurrentUser()
    {
        var keycloakId = GetCurrentKeycloakId();
        var email = GetCurrentUserEmail();
        var firstName = GetCurrentUserFirstName();
        var lastName = GetCurrentUserLastName();

        var user = await _context.Users
            .Include(u => u.UserProfile)
            .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

        if (user == null)
        {
            // Create new user from token info
            user = new User
            {
                KeycloakId = keycloakId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = GetCurrentUserRole(), // Use role from Keycloak token
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update existing user from token info
            user.Email = email;
            user.FirstName = firstName;
            user.LastName = lastName;
            await SyncLinkedIdentityEmailAsync(user.KeycloakId, email);
            await _context.SaveChangesAsync();
        }

        return Ok(_mapper.Map<UserDto>(user));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Invalid user ID in token");
    }

    private async Task SyncLinkedIdentityEmailAsync(string keycloakId, string email)
    {
        var accountHolder = await _context.AccountHolders.FirstOrDefaultAsync(a => a.KeycloakUserId == keycloakId);
        if (accountHolder != null)
        {
            accountHolder.EmailAddress = email;
            accountHolder.LastEdit = DateTime.UtcNow;
            accountHolder.UpdatedAt = DateTime.UtcNow;
        }

        var educators = await _context.Educators.Where(e => e.KeycloakUserId == keycloakId).ToListAsync();
        foreach (var educator in educators)
        {
            educator.Email = email;
            educator.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task EnsureEmailIsAvailableAsync(User user, string normalizedEmail)
    {
        var normalizedEmailLower = normalizedEmail.ToLowerInvariant();

        var duplicateUserExists = await _context.Users.AnyAsync(u =>
            u.TenantId == user.TenantId &&
            u.Id != user.Id &&
            u.Email.ToLower() == normalizedEmailLower);
        if (duplicateUserExists)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var duplicatePendingUserExists = await _context.Users.AnyAsync(u =>
            u.TenantId == user.TenantId &&
            u.Id != user.Id &&
            u.PendingEmail != null &&
            u.PendingEmail.ToLower() == normalizedEmailLower);
        if (duplicatePendingUserExists)
        {
            throw new InvalidOperationException("Another email change is already pending for this address");
        }

        var duplicateAccountHolderExists = await _context.AccountHolders.AnyAsync(a =>
            a.TenantId == user.TenantId &&
            a.KeycloakUserId != user.KeycloakId &&
            a.EmailAddress.ToLower() == normalizedEmailLower);
        if (duplicateAccountHolderExists)
        {
            throw new InvalidOperationException("Account holder with this email already exists");
        }
    }

    private static void ClearPendingEmailChange(User user)
    {
        user.PendingEmail = null;
        user.PendingEmailTokenHash = null;
        user.PendingEmailRequestedAtUtc = null;
        user.PendingEmailExpiresAtUtc = null;
    }

    private string BuildEmailChangeConfirmationUrl(string token)
    {
        var frontendBaseUrl = ResolveFrontendBaseUrl();
        var tenantSlug = _tenantContextAccessor.TenantContext?.Tenant?.Subdomain;
        var path = BuildTenantPath("/confirm-email-change", tenantSlug);
        return $"{frontendBaseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }

    private string ResolveFrontendBaseUrl()
    {
        var origin = Request.Headers.Origin.FirstOrDefault();
        if (Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return originUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        var referer = Request.Headers.Referer.FirstOrDefault();
        if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
            return refererUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        var configuredBaseUrl = _configuration["Tenancy:AppBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl.TrimEnd('/');
        }

        throw new InvalidOperationException("Unable to determine the tenant app base URL for email confirmation.");
    }

    private static string BuildTenantPath(string targetPath, string? tenantSlug)
    {
        var normalizedTargetPath = string.IsNullOrWhiteSpace(targetPath) || targetPath == "/"
            ? "/"
            : targetPath.StartsWith('/') ? targetPath : $"/{targetPath}";

        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            return normalizedTargetPath;
        }

        if (normalizedTargetPath == "/")
        {
            return $"/org/{tenantSlug}";
        }

        return $"/org/{tenantSlug}{normalizedTargetPath}";
    }

    private static string GenerateEmailChangeToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    private static string HashEmailChangeToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token.Trim());
        return Convert.ToHexString(SHA256.HashData(tokenBytes)).ToLowerInvariant();
    }

    private static string? NormalizeEmail(string? email)
    {
        if (email == null)
        {
            return null;
        }

        var normalizedEmail = email.Trim();
        return normalizedEmail.Length == 0 ? null : normalizedEmail;
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

    private UserRole GetCurrentUserRole()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        if (roles.Contains("Administrator"))
            return UserRole.Administrator;
        if (roles.Contains("Educator"))
            return UserRole.Educator;
        if (roles.Contains("Member"))
            return UserRole.Member;
        
        return UserRole.Member;
    }

    private IEnumerable<string> GetCurrentUserRoles()
    {
        // Get roles from standard claims
        var roleClaims = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        // Also check for realm_access roles (Keycloak specific)
        var realmAccessClaim = User.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrEmpty(realmAccessClaim))
        {
            try
            {
                var realmAccess = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(realmAccessClaim);
                if (realmAccess != null && realmAccess.ContainsKey("roles"))
                {
                    var rolesElement = (System.Text.Json.JsonElement)realmAccess["roles"];
                    if (rolesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var realmRoles = rolesElement.EnumerateArray()
                            .Where(r => r.ValueKind == System.Text.Json.JsonValueKind.String)
                            .Select(r => r.GetString())
                            .Where(r => !string.IsNullOrEmpty(r))
                            .Cast<string>();
                        
                        roleClaims.AddRange(realmRoles);
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Ignore JSON parsing errors
            }
        }
        
        // Remove duplicates and return
        return roleClaims.Distinct().Where(r => !string.IsNullOrEmpty(r));
    }
}
