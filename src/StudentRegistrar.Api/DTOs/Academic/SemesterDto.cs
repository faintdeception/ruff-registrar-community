using System.ComponentModel.DataAnnotations;

namespace StudentRegistrar.Api.DTOs;

public class SemesterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime RegistrationStartDate { get; set; }
    public DateTime RegistrationEndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsRegistrationOpen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CourseDto> Courses { get; set; } = new();
}

public class CreateSemesterDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime RegistrationStartDate { get; set; }
    public DateTime RegistrationEndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSemesterDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime RegistrationStartDate { get; set; }
    public DateTime RegistrationEndDate { get; set; }
    public bool IsActive { get; set; }
}
