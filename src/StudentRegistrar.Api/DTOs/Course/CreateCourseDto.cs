using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class CreateCourseDto
{
    public Guid SemesterId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? RoomId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MaxCapacity must be at least 1.")]
    public int MaxCapacity { get; set; }

    public decimal Fee { get; set; } = 0;
    public string? PeriodCode { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    [Required]
    public string AgeGroup { get; set; } = string.Empty;
}
