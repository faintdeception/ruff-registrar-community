using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;

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

    // Legacy integer ID endpoints - will be deprecated
    [HttpGet("legacy/{id:int}")]
    [Obsolete("Use GetCourse(Guid id) instead")]
    public async Task<ActionResult<CourseDto>> GetCourseLegacy(int id)
    {
        // Legacy bridge - try to find by hash code
        var courses = await _courseService.GetAllCoursesAsync();
        var course = courses.FirstOrDefault(c => c.Id.GetHashCode() == id);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    [HttpPut("legacy/{id:int}")]
    [Obsolete("Use UpdateCourse(Guid id, UpdateCourseDto) instead")]
    public async Task<ActionResult<CourseDto>> UpdateCourseLegacy(int id, UpdateCourseDto updateCourseDto)
    {
        // Legacy bridge - try to find by hash code
        var courses = await _courseService.GetAllCoursesAsync();
        var course = courses.FirstOrDefault(c => c.Id.GetHashCode() == id);
        if (course == null)
            return NotFound();

        return await UpdateCourse(course.Id, updateCourseDto);
    }

    [HttpDelete("legacy/{id:int}")]
    [Obsolete("Use DeleteCourse(Guid id) instead")]
    public async Task<IActionResult> DeleteCourseLegacy(int id)
    {
        // Legacy bridge - try to find by hash code
        var courses = await _courseService.GetAllCoursesAsync();
        var course = courses.FirstOrDefault(c => c.Id.GetHashCode() == id);
        if (course == null)
            return NotFound();

        return await DeleteCourse(course.Id);
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
