using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using System.Security.Claims;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseServiceV2 _courseService;

    public CoursesController(ICourseServiceV2 courseService)
    {
        _courseService = courseService;
    }

    // Modern Guid-based endpoints
    [HttpGet]
    [AllowAnonymous] // Allow anonymous access to view courses
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return Ok(courses);
    }

    [HttpGet("semester/{semesterId:guid}")]
    [AllowAnonymous] // Allow anonymous access to view courses by semester
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetCoursesBySemester(Guid semesterId)
    {
        var courses = await _courseService.GetCoursesBySemesterAsync(semesterId);
        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous] // Allow anonymous access to view a course
    public async Task<ActionResult<CourseDto>> GetCourse(Guid id)
    {
        var course = await _courseService.GetCourseByIdAsync(id);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    [HttpPost]
    public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto createCourseDto)
    {
        var course = await _courseService.CreateCourseAsync(createCourseDto);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    [HttpPost("{courseId:guid}/enrollments")]
    public async Task<ActionResult<CourseEnrollmentResultDto>> EnrollStudent(Guid courseId, CreateCourseEnrollmentDto createEnrollmentDto)
    {
        var keycloakUserId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            var result = await _courseService.EnrollStudentAsync(courseId, createEnrollmentDto, keycloakUserId ?? string.Empty);
            return CreatedAtAction(nameof(GetCourse), new { id = courseId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CourseDto>> UpdateCourse(Guid id, UpdateCourseDto updateCourseDto)
    {
        var course = await _courseService.UpdateCourseAsync(id, updateCourseDto);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        var result = await _courseService.DeleteCourseAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    // Instructor Management Endpoints
    [HttpGet("{courseId:guid}/instructors")]
    public async Task<ActionResult<IEnumerable<CourseInstructorDto>>> GetCourseInstructors(Guid courseId)
    {
        var instructors = await _courseService.GetCourseInstructorsAsync(courseId);
        return Ok(instructors);
    }

    [HttpPost("{courseId:guid}/instructors")]
    public async Task<ActionResult<CourseInstructorDto>> AddInstructor(Guid courseId, CreateCourseInstructorDto createInstructorDto)
    {
        if (createInstructorDto.CourseId != courseId)
        {
            createInstructorDto.CourseId = courseId; // Ensure consistency
        }

        var instructor = await _courseService.AddInstructorAsync(createInstructorDto);
        return CreatedAtAction(nameof(GetCourseInstructors), new { courseId }, instructor);
    }

    [HttpPut("{courseId:guid}/instructors/{instructorId:guid}")]
    public async Task<ActionResult<CourseInstructorDto>> UpdateInstructor(
        Guid courseId, 
        Guid instructorId, 
        UpdateCourseInstructorDto updateInstructorDto)
    {
        var instructor = await _courseService.UpdateInstructorAsync(instructorId, updateInstructorDto);
        if (instructor == null)
            return NotFound();

        return Ok(instructor);
    }

    [HttpDelete("{courseId:guid}/instructors/{instructorId:guid}")]
    public async Task<IActionResult> RemoveInstructor(Guid courseId, Guid instructorId)
    {
        var result = await _courseService.RemoveInstructorAsync(instructorId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    // Endpoint to get available members who can be instructors
    [HttpGet("available-members")]
    public async Task<ActionResult<IEnumerable<AccountHolderDto>>> GetAvailableMembers()
    {
        var members = await _courseService.GetAvailableMembersAsync();
        return Ok(members);
    }
}
