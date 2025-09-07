using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SemestersController : ControllerBase
{
    private readonly ISemesterService _semesterService;
    private readonly ILogger<SemestersController> _logger;

    public SemestersController(
        ISemesterService semesterService,
        ILogger<SemestersController> logger)
    {
        _semesterService = semesterService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous] // Allow anonymous access to view semesters
    public async Task<ActionResult<IEnumerable<SemesterDto>>> GetSemesters()
    {
        try
        {
            var semesters = await _semesterService.GetAllSemestersAsync();
            return Ok(semesters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving semesters");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous] // Allow anonymous access to view a semester
    public async Task<ActionResult<SemesterDto>> GetSemester(Guid id)
    {
        try
        {
            var semester = await _semesterService.GetSemesterByIdAsync(id);
            if (semester == null)
                return NotFound();

            return Ok(semester);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving semester {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("active")]
    [AllowAnonymous] // Allow anonymous access to view the active semester
    public async Task<ActionResult<SemesterDto>> GetActiveSemester()
    {
        try
        {
            var semester = await _semesterService.GetActiveSemesterAsync();
            if (semester == null)
                return NotFound("No active semester found");

            return Ok(semester);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active semester");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<SemesterDto>> CreateSemester(CreateSemesterDto createDto)
    {
        try
        {
            var semester = await _semesterService.CreateSemesterAsync(createDto);
            return CreatedAtAction(nameof(GetSemester), new { id = semester.Id }, semester);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating semester");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<SemesterDto>> UpdateSemester(Guid id, UpdateSemesterDto updateDto)
    {
        try
        {
            var semester = await _semesterService.UpdateSemesterAsync(id, updateDto);
            if (semester == null)
                return NotFound();

            return Ok(semester);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating semester {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> DeleteSemester(Guid id)
    {
        try
        {
            var deleted = await _semesterService.DeleteSemesterAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting semester {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
