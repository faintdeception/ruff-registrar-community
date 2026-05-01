using System.Reflection;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Contracts;

public class GuidContractTests
{
    private readonly IMapper _mapper;

    public GuidContractTests()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = mapperConfig.CreateMapper();
    }

    [Fact]
    public void StudentDto_Should_Preserve_Student_Guid_Id()
    {
        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace"
        };

        var dto = _mapper.Map<StudentDto>(student);

        dto.Id.Should().Be(student.Id);
    }

    [Fact]
    public void EnrollmentDto_Should_Preserve_Guid_Identifiers()
    {
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            EnrollmentType = EnrollmentType.Enrolled,
            EnrollmentDate = DateTime.UtcNow
        };

        var dto = _mapper.Map<EnrollmentDto>(enrollment);

        dto.Id.Should().Be(enrollment.Id);
        dto.StudentId.Should().Be(enrollment.StudentId);
        dto.CourseId.Should().Be(enrollment.CourseId);
    }

    [Fact]
    public void CreateEnrollmentDto_Should_Map_Guid_Identifiers_To_Model()
    {
        var createDto = new CreateEnrollmentDto
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            EnrollmentDate = DateTime.UtcNow
        };

        var enrollment = _mapper.Map<Enrollment>(createDto);

        enrollment.StudentId.Should().Be(createDto.StudentId);
        enrollment.CourseId.Should().Be(createDto.CourseId);
    }

    [Fact]
    public void GradeRecordDto_Should_Preserve_Student_And_Course_Guid_Identifiers()
    {
        var grade = new GradeRecord
        {
            Id = 42,
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            GradeDate = DateTime.UtcNow
        };

        var dto = _mapper.Map<GradeRecordDto>(grade);

        dto.Id.Should().Be(grade.Id);
        dto.StudentId.Should().Be(grade.StudentId);
        dto.CourseId.Should().Be(grade.CourseId);
    }

    [Fact]
    public void CreateGradeRecordDto_Should_Map_Guid_Identifiers_To_Model()
    {
        var createDto = new CreateGradeRecordDto
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            NumericGrade = 95,
            GradeDate = DateTime.UtcNow
        };

        var grade = _mapper.Map<GradeRecord>(createDto);

        grade.StudentId.Should().Be(createDto.StudentId);
        grade.CourseId.Should().Be(createDto.CourseId);
    }

    [Fact]
    public void CoursesController_Should_Not_Expose_Legacy_Integer_Routes()
    {
        var routeTemplates = GetRouteTemplates<CoursesController>();

        routeTemplates.Should().NotContain(template => template.Contains("legacy", StringComparison.OrdinalIgnoreCase));
        routeTemplates.Should().Contain("{id:guid}");
    }

    [Fact]
    public void StudentsController_Should_Constrain_Entity_Routes_To_Guid_Ids()
    {
        var routeTemplates = GetRouteTemplates<StudentsController>();

        routeTemplates.Should().Contain("{id:guid}");
        routeTemplates.Should().Contain("by-account/{accountHolderId:guid}");
    }

    private static IReadOnlyCollection<string> GetRouteTemplates<TController>()
    {
        return typeof(TController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .Select(attribute => attribute.Template)
            .OfType<string>()
            .ToArray();
    }
}