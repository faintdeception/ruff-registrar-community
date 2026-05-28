using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
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
        _mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();
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

        Assert.Equal(student.Id, dto.Id);
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

        Assert.Equal(enrollment.Id, dto.Id);
        Assert.Equal(enrollment.StudentId, dto.StudentId);
        Assert.Equal(enrollment.CourseId, dto.CourseId);
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

        Assert.Equal(createDto.StudentId, enrollment.StudentId);
        Assert.Equal(createDto.CourseId, enrollment.CourseId);
    }

    [Fact]
    public void GradeRecordDto_Should_Preserve_Student_And_Course_Guid_Identifiers()
    {
        var grade = new GradeRecord
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            GradeDate = DateTime.UtcNow
        };

        var dto = _mapper.Map<GradeRecordDto>(grade);

        Assert.Equal(grade.Id, dto.Id);
        Assert.Equal(grade.StudentId, dto.StudentId);
        Assert.Equal(grade.CourseId, dto.CourseId);
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

        Assert.Equal(createDto.StudentId, grade.StudentId);
        Assert.Equal(createDto.CourseId, grade.CourseId);
    }

    [Fact]
    public void CoursesController_Should_Not_Expose_Legacy_Integer_Routes()
    {
        var routeTemplates = GetRouteTemplates<CoursesController>();

        Assert.DoesNotContain(routeTemplates, template => template.Contains("legacy", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("{id:guid}", routeTemplates);
    }

    [Fact]
    public void StudentsController_Should_Constrain_Entity_Routes_To_Guid_Ids()
    {
        var routeTemplates = GetRouteTemplates<StudentsController>();

        Assert.Contains("{id:guid}", routeTemplates);
        Assert.Contains("by-account/{accountHolderId:guid}", routeTemplates);
    }

    [Fact]
    public void GradesController_Should_Constrain_Grade_Routes_To_Guid_Ids()
    {
        var routeTemplates = GetRouteTemplates<GradesController>();

        Assert.Contains("{id:guid}", routeTemplates);
        Assert.DoesNotContain("{id:int}", routeTemplates);
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