using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class StudentsControllerTests
{
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly StudentsController _controller;

    public StudentsControllerTests()
    {
        _mockStudentService = new Mock<IStudentService>();
        _controller = new StudentsController(_mockStudentService.Object);
    }

    [Fact]
    public async Task GetStudents_Should_ReturnOkWithStudents()
    {
        // Arrange
        var expectedStudents = new List<StudentDto>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith" }
        };

        _mockStudentService
            .Setup(s => s.GetAllStudentsAsync())
            .Returns(Task.FromResult<IEnumerable<StudentDto>>(expectedStudents));

        // Act
        var result = await _controller.GetStudents();

        // Assert
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedStudents);
    }

    [Fact]
    public async Task GetStudent_Should_ReturnOkWithStudent_WhenStudentExists()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var expectedStudent = new StudentDto 
        { 
            Id = 1, 
            FirstName = "John", 
            LastName = "Doe" 
        };

        _mockStudentService
            .Setup(s => s.GetStudentByIdAsync(studentId))
            .Returns(Task.FromResult<StudentDto?>(expectedStudent));

        // Act
        var result = await _controller.GetStudent(studentId);

        // Assert
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedStudent);
    }

    [Fact]
    public async Task GetStudent_Should_ReturnNotFound_WhenStudentDoesNotExist()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        _mockStudentService
            .Setup(s => s.GetStudentByIdAsync(studentId))
            .Returns(Task.FromResult<StudentDto?>(null));

        // Act
        var result = await _controller.GetStudent(studentId);

        // Assert
        var actionResult = result.Result;
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateStudent_Should_ReturnCreatedStudent()
    {
        // Arrange
        var createDto = new CreateStudentDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
        };

        var createdStudent = new StudentDto
        {
            Id = 1,
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email,
            DateOfBirth = createDto.DateOfBirth
        };

        _mockStudentService
            .Setup(s => s.CreateStudentAsync(createDto))
            .Returns(Task.FromResult(createdStudent));

        // Act
        var result = await _controller.CreateStudent(createDto);

        // Assert
        var actionResult = result.Result;
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdStudent);
    }

    [Fact]
    public async Task UpdateStudent_Should_ReturnOkWithUpdatedStudent_WhenStudentExists()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var updateDto = new UpdateStudentDto
        {
            FirstName = "John Updated",
            LastName = "Doe Updated",
            Email = "john.updated@example.com",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-21))
        };

        var updatedStudent = new StudentDto
        {
            Id = 1,
            FirstName = updateDto.FirstName,
            LastName = updateDto.LastName,
            Email = updateDto.Email,
            DateOfBirth = updateDto.DateOfBirth
        };

        _mockStudentService
            .Setup(s => s.UpdateStudentAsync(studentId, updateDto))
            .Returns(Task.FromResult<StudentDto?>(updatedStudent));

        // Act
        var result = await _controller.UpdateStudent(studentId, updateDto);

        // Assert
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(updatedStudent);
    }

    [Fact]
    public async Task UpdateStudent_Should_ReturnNotFound_WhenStudentDoesNotExist()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var updateDto = new UpdateStudentDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20))
        };

        _mockStudentService
            .Setup(s => s.UpdateStudentAsync(studentId, updateDto))
            .Returns(Task.FromResult<StudentDto?>(null));

        // Act
        var result = await _controller.UpdateStudent(studentId, updateDto);

        // Assert
        var actionResult = result.Result;
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteStudent_Should_ReturnNoContent_WhenStudentDeletedSuccessfully()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        _mockStudentService
            .Setup(s => s.DeleteStudentAsync(studentId))
            .Returns(Task.FromResult(true));

        // Act
        var result = await _controller.DeleteStudent(studentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteStudent_Should_ReturnNotFound_WhenStudentNotFound()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        _mockStudentService
            .Setup(s => s.DeleteStudentAsync(studentId))
            .Returns(Task.FromResult(false));

        // Act
        var result = await _controller.DeleteStudent(studentId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetStudentsByAccountHolder_Should_ReturnStudentsForAccountHolder()
    {
        // Arrange
        var accountHolderId = Guid.NewGuid();
        var expectedStudents = new List<StudentDto>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe" },
            new() { Id = 2, FirstName = "Jane", LastName = "Doe" }
        };

        _mockStudentService
            .Setup(s => s.GetStudentsByAccountHolderAsync(accountHolderId))
            .Returns(Task.FromResult<IEnumerable<StudentDto>>(expectedStudents));

        // Act
        var result = await _controller.GetStudentsByAccountHolder(accountHolderId);

        // Assert
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedStudents);
    }
}
