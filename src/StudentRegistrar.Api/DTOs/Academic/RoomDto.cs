using System.ComponentModel.DataAnnotations;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.DTOs;

public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Notes { get; set; }
    public RoomType RoomType { get; set; }
    public int CourseCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateRoomDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 1000)]
    public int Capacity { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    [Required]
    public RoomType RoomType { get; set; } = RoomType.Classroom;
}

public class UpdateRoomDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 1000)]
    public int Capacity { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    [Required]
    public RoomType RoomType { get; set; }
}
