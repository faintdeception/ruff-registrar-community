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
            if (!IsAdministrator())
            {
                return ForbiddenMessage("Only administrators can invite educators");
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
            if (!IsAdministrator())
            {
                return ForbiddenMessage("Only administrators can create educators");
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
            if (!IsAdministrator())
            {
                return ForbiddenMessage("Only administrators can update educators");
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
            if (!IsAdministrator())
            {
                return ForbiddenMessage("Only administrators can delete educators");
            }

            var result = await _educatorService.DeleteEducatorAsync(id);
            return result switch
            {
                DeleteEducatorResult.NotFound => NotFound(),
                DeleteEducatorResult.SoftDeleted => Ok(new { softDeleted = true }),
                DeleteEducatorResult.HardDeleted => NoContent(),
                _ => StatusCode(500, "Unexpected delete result")
            };
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
            if (!IsAdministrator())
            {
                return ForbiddenMessage("Only administrators can deactivate educators");
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
            if (!IsAdministrator())
            {
                return ForbiddenMessage("Only administrators can activate educators");
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

    private ActionResult ForbiddenMessage(string message)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { error = message });
    }

    private bool IsAdministrator()
    {
        if (User.IsInRole("Administrator"))
        {
            return true;
        }

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
            return false;

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
                        return roles?.Contains("Administrator") == true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JWT token roles");
            }
        }

        return false;
    }
}
