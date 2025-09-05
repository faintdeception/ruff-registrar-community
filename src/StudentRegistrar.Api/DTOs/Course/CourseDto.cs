using StudentRegistrar.Models;

namespace StudentRegistrar.Api.DTOs;

public class CourseDto
{
    public Guid Id { get; set; }
    public Guid SemesterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? RoomId { get; set; }
    public RoomDto? Room { get; set; }
    public int MaxCapacity { get; set; }
    public decimal Fee { get; set; }
    public string? PeriodCode { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? TimeSlot { get; set; }
    public string AgeGroup { get; set; } = string.Empty;
    public int CurrentEnrollment { get; set; }
    public int AvailableSpots { get; set; }
    public bool IsFull { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public SemesterDto? Semester { get; set; }
    public List<CourseInstructorDto> Instructors { get; set; } = new();
    public List<string> InstructorNames { get; set; } = new();
}
