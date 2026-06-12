using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using System.Security.Claims;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly StudentRegistrarDbContext _context;
    private readonly IMapper _mapper;
    private readonly IKeycloakService _keycloakService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        StudentRegistrarDbContext context, 
        IMapper mapper, 
        IKeycloakService keycloakService,
        ILogger<UsersController> logger)
    {
        _context = context;
        _mapper = mapper;
        _keycloakService = keycloakService;
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
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();
        
        // Users can only update their own profile unless they're admin
        if (currentUserId != id && currentUserRole != UserRole.Administrator)
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

        try
        {
            // Update user properties
            _mapper.Map(request, user);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, "Error updating user");
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
