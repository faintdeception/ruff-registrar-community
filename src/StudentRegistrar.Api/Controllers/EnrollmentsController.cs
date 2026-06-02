using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ILogger<EnrollmentsController> _logger;

    public EnrollmentsController(IEnrollmentService enrollmentService, ILogger<EnrollmentsController> logger)
    {
        _enrollmentService = enrollmentService;
        _logger = logger;
    }

    /// <summary>
    /// List enrollments. Admin only. Supports optional filters: courseId, studentId, semesterId, type.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEnrollments(
        [FromQuery] Guid? courseId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? semesterId = null,
        [FromQuery] EnrollmentType? type = null)
    {
        if (GetUserRole() != "Administrator")
            return Forbid();

        try
        {
            var enrollments = await _enrollmentService.GetAllAsync(courseId, studentId, semesterId, type);
            return Ok(enrollments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollments");
            return StatusCode(500, new { error = "An error occurred while retrieving enrollments." });
        }
    }

    /// <summary>
    /// Get a single enrollment by id. Admin or the account holder who owns the student.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEnrollment(Guid id)
    {
        try
        {
            var enrollment = await _enrollmentService.GetByIdAsync(id);
            if (enrollment is null)
                return NotFound(new { error = "Enrollment not found." });

            var role = GetUserRole();
            if (role != "Administrator")
            {
                // Members may only view their own students' enrollments — service does
                // not enforce this for reads, so we compare the studentId from the DTO
                // against the caller's keycloakUserId-linked students via GetMyEnrollmentsAsync.
                var keycloakUserId = GetKeycloakUserId();
                if (string.IsNullOrEmpty(keycloakUserId))
                    return Forbid();

                var mine = await _enrollmentService.GetMyEnrollmentsAsync(keycloakUserId);
                if (!mine.Any(e => e.Id == enrollment.Id))
                    return Forbid();
            }

            return Ok(enrollment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollment {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the enrollment." });
        }
    }

    /// <summary>
    /// Get all enrollments for the calling member's students.
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyEnrollments()
    {
        var keycloakUserId = GetKeycloakUserId();
        if (string.IsNullOrEmpty(keycloakUserId))
            return Unauthorized(new { error = "Unable to identify user." });

        try
        {
            var enrollments = await _enrollmentService.GetMyEnrollmentsAsync(keycloakUserId);
            return Ok(enrollments);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account holder not found for user {UserId}", keycloakUserId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollments for user {UserId}", keycloakUserId);
            return StatusCode(500, new { error = "An error occurred while retrieving your enrollments." });
        }
    }

    /// <summary>
    /// Get roster entries for the courses taught by the calling educator.
    /// </summary>
    [HttpGet("teaching")]
    public async Task<IActionResult> GetMyTeachingRoster([FromQuery] Guid? courseId = null)
    {
        if (GetUserRole() != "Educator")
            return Forbid();

        var keycloakUserId = GetKeycloakUserId();
        if (string.IsNullOrEmpty(keycloakUserId))
            return Unauthorized(new { error = "Unable to identify user." });

        try
        {
            var roster = await _enrollmentService.GetMyTeachingRosterAsync(keycloakUserId, courseId);
            return Ok(roster);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Educator roster lookup failed for user {UserId}", keycloakUserId);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized roster access by educator {UserId} for course {CourseId}", keycloakUserId, courseId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving teaching roster for user {UserId}", keycloakUserId);
            return StatusCode(500, new { error = "An error occurred while retrieving the teaching roster." });
        }
    }

    /// <summary>
    /// Get enrollments for a specific student. Admin or the student's account holder.
    /// </summary>
    [HttpGet("student/{studentId:guid}")]
    public async Task<IActionResult> GetByStudent(Guid studentId)
    {
        var role = GetUserRole();
        if (role != "Administrator")
        {
            var keycloakUserId = GetKeycloakUserId();
            if (string.IsNullOrEmpty(keycloakUserId))
                return Forbid();

            // Verify caller owns this student by checking their enrollment list.
            var mine = await _enrollmentService.GetMyEnrollmentsAsync(keycloakUserId);
            if (!mine.Any(e => e.StudentId == studentId.ToString()))
                return Forbid();
        }

        try
        {
            var enrollments = await _enrollmentService.GetByStudentAsync(studentId);
            return Ok(enrollments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollments for student {StudentId}", studentId);
            return StatusCode(500, new { error = "An error occurred while retrieving student enrollments." });
        }
    }

    /// <summary>
    /// Get enrollments for a specific course. Admin only.
    /// </summary>
    [HttpGet("course/{courseId:guid}")]
    public async Task<IActionResult> GetByCourse(Guid courseId)
    {
        if (GetUserRole() != "Administrator")
            return Forbid();

        try
        {
            var enrollments = await _enrollmentService.GetByCourseAsync(courseId);
            return Ok(enrollments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollments for course {CourseId}", courseId);
            return StatusCode(500, new { error = "An error occurred while retrieving course enrollments." });
        }
    }

    /// <summary>
    /// Withdraw an enrollment (Enrolled or Waitlisted → Withdrawn).
    /// Members may withdraw their own students' enrollments; admins may withdraw any.
    /// </summary>
    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawEnrollmentRequest? request = null)
    {
        var role = GetUserRole();
        // For admins, keycloakUserId is null so the service skips ownership check.
        string? keycloakUserId = role == "Administrator" ? null : GetKeycloakUserId();

        if (keycloakUserId is null && role != "Administrator")
            return Forbid();

        try
        {
            var result = await _enrollmentService.WithdrawAsync(id, keycloakUserId, request?.Reason);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized withdraw attempt on enrollment {Id}", id);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing enrollment {Id}", id);
            return StatusCode(500, new { error = "An error occurred while withdrawing the enrollment." });
        }
    }

    /// <summary>
    /// Cancel an enrollment. Admin only.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        if (GetUserRole() != "Administrator")
            return Forbid();

        try
        {
            var result = await _enrollmentService.CancelAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling enrollment {Id}", id);
            return StatusCode(500, new { error = "An error occurred while cancelling the enrollment." });
        }
    }

    /// <summary>
    /// Promote a waitlisted enrollment to Enrolled. Admin only.
    /// </summary>
    [HttpPost("{id:guid}/promote")]
    public async Task<IActionResult> Promote(Guid id)
    {
        if (GetUserRole() != "Administrator")
            return Forbid();

        try
        {
            var result = await _enrollmentService.PromoteFromWaitlistAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting enrollment {Id}", id);
            return StatusCode(500, new { error = "An error occurred while promoting the enrollment." });
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string GetKeycloakUserId()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;
    }

    private string GetUserRole()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
            return string.Empty;

        var token = authHeader["Bearer ".Length..];
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "realm_access");
        if (roleClaim != null)
        {
            try
            {
                var realmAccess = JsonSerializer.Deserialize<Dictionary<string, object>>(roleClaim.Value);
                if (realmAccess != null && realmAccess.TryGetValue("roles", out var rolesObj))
                {
                    var roles = JsonSerializer.Deserialize<string[]>(rolesObj.ToString()!);
                    if (roles != null)
                    {
                        if (roles.Contains("Administrator")) return "Administrator";
                        if (roles.Contains("Educator")) return "Educator";
                        if (roles.Contains("Member")) return "Member";
                    }
                }
            }
            catch (JsonException)
            {
                // Fall through to claim-based check.
            }
        }

        return jsonToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value
            ?? jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
            ?? string.Empty;
    }
}

/// <summary>Optional body for the withdraw action.</summary>
public sealed class WithdrawEnrollmentRequest
{
    public string? Reason { get; set; }
}
