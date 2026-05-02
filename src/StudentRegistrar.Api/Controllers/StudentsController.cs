using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudents()
    {
        var students = await _studentService.GetAllStudentsAsync();
        return Ok(students);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentDto>> GetStudent(Guid id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);
        if (student == null)
            return NotFound();

        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<StudentDto>> CreateStudent(CreateStudentDto createStudentDto)
    {
        var student = await _studentService.CreateStudentAsync(createStudentDto);
        return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StudentDto>> UpdateStudent(Guid id, UpdateStudentDto updateStudentDto)
    {
        var student = await _studentService.UpdateStudentAsync(id, updateStudentDto);
        if (student == null)
            return NotFound();

        return Ok(student);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStudent(Guid id)
    {
        var result = await _studentService.DeleteStudentAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    // New endpoint for account holder's students
    [HttpGet("by-account/{accountHolderId:guid}")]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudentsByAccountHolder(Guid accountHolderId)
    {
        var students = await _studentService.GetStudentsByAccountHolderAsync(accountHolderId);
        return Ok(students);
    }
}
