using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using System.IdentityModel.Tokens.Jwt;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EducatorsController : ControllerBase
{
    private readonly IEducatorService _educatorService;
    private readonly ILogger<EducatorsController> _logger;

    public EducatorsController(
        IEducatorService educatorService,
        ILogger<EducatorsController> logger)
    {
        _educatorService = educatorService;
        _logger = logger;
    }

    [HttpPost("invite")]
    public async Task<ActionResult<InviteEducatorResponse>> InviteEducator(InviteEducatorDto inviteDto)
    {
        try
        {
            var userRole = GetUserRole();
            if (userRole != "Administrator")
            {
                return Forbid("Only administrators can invite educators");
            }

            var invitation = await _educatorService.InviteEducatorAsync(inviteDto);
            return CreatedAtAction(nameof(GetEducator), new { id = invitation.Educator.Id }, invitation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting educator");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EducatorDto>>> GetEducators()
    {
        try
        {
            // Allow all authenticated users to view educators
            var educators = await _educatorService.GetAllEducatorsAsync();
            return Ok(educators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving educators");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EducatorDto>> GetEducator(Guid id)
    {
        try
        {
            // Allow all authenticated users to view educator details
            var educator = await _educatorService.GetEducatorByIdAsync(id);
            if (educator == null)
                return NotFound();

            return Ok(educator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving educator {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<EducatorDto>> CreateEducator(CreateEducatorDto createDto)
    {
        try
        {
            // Validate user has admin role
            var userRole = GetUserRole();
            if (userRole != "Administrator")
            {
                return Forbid("Only administrators can create educators");
            }

            var educator = await _educatorService.CreateEducatorAsync(createDto);
            return CreatedAtAction(nameof(GetEducator), new { id = educator.Id }, educator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating educator");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EducatorDto>> UpdateEducator(Guid id, UpdateEducatorDto updateDto)
    {
        try
        {
            // Validate user has admin role
            var userRole = GetUserRole();
            if (userRole != "Administrator")
            {
                return Forbid("Only administrators can update educators");
            }

            var educator = await _educatorService.UpdateEducatorAsync(id, updateDto);
            if (educator == null)
                return NotFound();

            return Ok(educator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating educator {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEducator(Guid id)
    {
        try
        {
            // Validate user has admin role
            var userRole = GetUserRole();
            if (userRole != "Administrator")
            {
                return Forbid("Only administrators can delete educators");
            }

            var deleted = await _educatorService.DeleteEducatorAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting educator {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult> DeactivateEducator(Guid id)
    {
        try
        {
            // Validate user has admin role
            var userRole = GetUserRole();
            if (userRole != "Administrator")
            {
                return Forbid("Only administrators can deactivate educators");
            }

            var success = await _educatorService.DeactivateEducatorAsync(id);
            if (!success)
                return NotFound();

            return Ok(new { message = "Educator deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating educator {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult> ActivateEducator(Guid id)
    {
        try
        {
            // Validate user has admin role
            var userRole = GetUserRole();
            if (userRole != "Administrator")
            {
                return Forbid("Only administrators can activate educators");
            }

            var success = await _educatorService.ActivateEducatorAsync(id);
            if (!success)
                return NotFound();

            return Ok(new { message = "Educator activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating educator {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<IEnumerable<EducatorDto>>> GetEducatorsByCourse(Guid courseId)
    {
        try
        {
            // Allow all authenticated users to view educators by course
            var educators = await _educatorService.GetEducatorsByCourseIdAsync(courseId);
            return Ok(educators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving educators for course {CourseId}", courseId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("unassigned")]
    public async Task<ActionResult<IEnumerable<EducatorDto>>> GetUnassignedEducators()
    {
        try
        {
            // Allow all authenticated users to view unassigned educators
            var educators = await _educatorService.GetUnassignedEducatorsAsync();
            return Ok(educators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unassigned educators");
            return StatusCode(500, "Internal server error");
        }
    }

    private string GetUserRole()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
            return "";

        var token = authHeader.Substring("Bearer ".Length);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        
        // Check for role claims (Keycloak uses different claim names)
        var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "realm_access");

        if (roleClaim != null)
        {
            try
            {
                // Parse the JSON to extract roles
                var realmAccess = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(roleClaim.Value);
                if (realmAccess != null && realmAccess.ContainsKey("roles"))
                {
                    var rolesObject = realmAccess["roles"];
                    if (rolesObject != null)
                    {
                        var roles = System.Text.Json.JsonSerializer.Deserialize<string[]>(rolesObject.ToString()!);
                        return roles?.FirstOrDefault(r => r == "Administrator" || r == "Member" || r == "Educator" || r == "Instructor") ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JWT token roles");
            }
        }

        return "";
    }
}
