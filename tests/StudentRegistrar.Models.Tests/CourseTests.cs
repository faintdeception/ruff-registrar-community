using StudentRegistrar.Models;
using System.Text.Json;
using Xunit;

namespace StudentRegistrar.Models.Tests;

public class CourseTests
{
    [Fact]
    public void Course_Should_HaveDefaultValues()
    {
        // Act
        var course = new Course();
        var now = DateTime.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, course.Id);
        Assert.Equal(Guid.Empty, course.SemesterId);
        Assert.Equal(string.Empty, course.Name);
        Assert.Null(course.Code);
        Assert.Null(course.Description);
        Assert.Null(course.Room);
        Assert.Equal(0, course.MaxCapacity);
        Assert.Equal(0, course.Fee);
        Assert.Null(course.PeriodCode);
        Assert.Null(course.StartTime);
        Assert.Null(course.EndTime);
        Assert.Equal("{}", course.CourseConfigJson);
        Assert.Equal(string.Empty, course.AgeGroup);
        Assert.InRange(course.CreatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.InRange(course.UpdatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.NotNull(course.CourseInstructors);
        Assert.Empty(course.CourseInstructors);
        Assert.NotNull(course.Enrollments);
        Assert.Empty(course.Enrollments);
    }

    [Fact]
    public void CurrentEnrollment_Should_CountOnlyEnrolledStudents()
    {
        // Arrange
        var course = new Course();
        var enrollments = new List<Enrollment>
        {
            new() { EnrollmentType = EnrollmentType.Enrolled },
            new() { EnrollmentType = EnrollmentType.Enrolled },
            new() { EnrollmentType = EnrollmentType.Waitlisted },
            new() { EnrollmentType = EnrollmentType.Withdrawn }
        };
        
        // Mock the enrollments collection behavior
        course.GetType().GetProperty(nameof(course.Enrollments))!
            .SetValue(course, enrollments);

        // Act & Assert
        Assert.Equal(2, course.CurrentEnrollment);
    }

    [Fact]
    public void AvailableSpots_Should_CalculateCorrectly()
    {
        // Arrange
        var course = new Course { MaxCapacity = 20 };
        var enrollments = new List<Enrollment>
        {
            new() { EnrollmentType = EnrollmentType.Enrolled },
            new() { EnrollmentType = EnrollmentType.Enrolled },
            new() { EnrollmentType = EnrollmentType.Enrolled }
        };
        
        course.GetType().GetProperty(nameof(course.Enrollments))!
            .SetValue(course, enrollments);

        // Act & Assert
        Assert.Equal(17, course.AvailableSpots);
    }

    [Fact]
    public void IsFull_Should_ReturnTrue_WhenAtCapacity()
    {
        // Arrange
        var course = new Course { MaxCapacity = 2 };
        var enrollments = new List<Enrollment>
        {
            new() { EnrollmentType = EnrollmentType.Enrolled },
            new() { EnrollmentType = EnrollmentType.Enrolled }
        };
        
        course.GetType().GetProperty(nameof(course.Enrollments))!
            .SetValue(course, enrollments);

        // Act & Assert
        Assert.True(course.IsFull);
    }

    [Fact]
    public void IsFull_Should_ReturnFalse_WhenBelowCapacity()
    {
        // Arrange
        var course = new Course { MaxCapacity = 3 };
        var enrollments = new List<Enrollment>
        {
            new() { EnrollmentType = EnrollmentType.Enrolled }
        };
        
        course.GetType().GetProperty(nameof(course.Enrollments))!
            .SetValue(course, enrollments);

        // Act & Assert
        Assert.False(course.IsFull);
    }

    [Fact]
    public void TimeSlot_Should_FormatCorrectly_WhenTimesAreSet()
    {
        // Arrange
        var course = new Course
        {
            StartTime = new TimeSpan(9, 0, 0),  // 9:00 AM
            EndTime = new TimeSpan(10, 30, 0)   // 10:30 AM
        };

        // Act & Assert
        Assert.Equal("09:00 - 10:30", course.TimeSlot);
    }

    [Fact]
    public void TimeSlot_Should_ShowTBD_WhenTimesAreNotSet()
    {
        // Arrange
        var course = new Course();

        // Act & Assert
        Assert.Equal("Time TBD", course.TimeSlot);
    }

    [Fact]
    public void TimeSlot_Should_ShowTBD_WhenOnlyOneTimeIsSet()
    {
        // Arrange
        var course = new Course
        {
            StartTime = new TimeSpan(9, 0, 0)
            // EndTime is null
        };

        // Act & Assert
        Assert.Equal("Time TBD", course.TimeSlot);
    }

    [Fact]
    public void GetCourseConfiguration_Should_ReturnValidConfig_WhenJsonIsValid()
    {
        // Arrange
        var course = new Course();
        var config = new CourseConfiguration
        {
            Prerequisites = new List<string> { "Basic Math", "Reading" },
            Materials = new List<string> { "Textbook", "Calculator" },
            DaysOfWeek = new List<string> { "Monday", "Wednesday", "Friday" },
            GradeRange = "6-8",
            CustomFields = new Dictionary<string, string> { { "SpecialNote", "Outdoor class" } }
        };
        course.SetCourseConfiguration(config);

        // Act
        var retrievedConfig = course.GetCourseConfiguration();

        // Assert
        Assert.NotNull(retrievedConfig);
        Assert.Equal(2, retrievedConfig.Prerequisites.Count);
        Assert.Contains("Basic Math", retrievedConfig.Prerequisites);
        Assert.Contains("Reading", retrievedConfig.Prerequisites);
        Assert.Contains("Textbook", retrievedConfig.Materials);
        Assert.Contains("Calculator", retrievedConfig.Materials);
        Assert.Equal(3, retrievedConfig.DaysOfWeek.Count);
        Assert.Contains("Monday", retrievedConfig.DaysOfWeek);
        Assert.Equal("6-8", retrievedConfig.GradeRange);
        Assert.Contains("SpecialNote", retrievedConfig.CustomFields.Keys);
        Assert.Equal("Outdoor class", retrievedConfig.CustomFields["SpecialNote"]);
    }

    [Fact]
    public void GetCourseConfiguration_Should_ReturnEmptyConfig_WhenJsonIsInvalid()
    {
        // Arrange
        var course = new Course
        {
            CourseConfigJson = "invalid json"
        };

        // Act
        var config = course.GetCourseConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Prerequisites);
        Assert.Empty(config.Prerequisites);
        Assert.NotNull(config.Materials);
        Assert.Empty(config.Materials);
        Assert.NotNull(config.DaysOfWeek);
        Assert.Empty(config.DaysOfWeek);
        Assert.Null(config.GradeRange);
        Assert.NotNull(config.CustomFields);
        Assert.Empty(config.CustomFields);
    }

    [Fact]
    public void SetCourseConfiguration_Should_SerializeCorrectly()
    {
        // Arrange
        var course = new Course();
        var config = new CourseConfiguration
        {
            Prerequisites = new List<string> { "Test Prerequisite" },
            GradeRange = "K-2"
        };

        // Act
        course.SetCourseConfiguration(config);

        // Assert
        Assert.NotEqual("{}", course.CourseConfigJson);
        
        // Verify we can deserialize it back
        var deserializedConfig = JsonSerializer.Deserialize<CourseConfiguration>(course.CourseConfigJson);
        Assert.NotNull(deserializedConfig);
        Assert.Contains("Test Prerequisite", deserializedConfig!.Prerequisites);
        Assert.Equal("K-2", deserializedConfig.GradeRange);
    }

    [Fact]
    public void CourseConfiguration_Should_InitializeEmptyCollections()
    {
        // Act
        var config = new CourseConfiguration();

        // Assert
        Assert.NotNull(config.Prerequisites);
        Assert.Empty(config.Prerequisites);
        Assert.NotNull(config.Materials);
        Assert.Empty(config.Materials);
        Assert.NotNull(config.DaysOfWeek);
        Assert.Empty(config.DaysOfWeek);
        Assert.NotNull(config.CustomFields);
        Assert.Empty(config.CustomFields);
    }

    [Fact]
    public void Course_Should_SupportDecimalFee()
    {
        // Arrange
        var course = new Course();
        var fee = 125.50m;

        // Act
        course.Fee = fee;

        // Assert
        Assert.Equal(fee, course.Fee);
    }

    [Fact]
    public void Course_Should_RequireSemesterId()
    {
        // Arrange
        var course = new Course();
        var semesterId = Guid.NewGuid();

        // Act
        course.SemesterId = semesterId;

        // Assert
        Assert.Equal(semesterId, course.SemesterId);
        Assert.NotEqual(Guid.Empty, course.SemesterId);
    }

    [Theory]
    [InlineData("Elementary")]
    [InlineData("Middle School")]
    [InlineData("High School")]
    [InlineData("Adult")]
    [InlineData("All Ages")]
    public void Course_Should_SupportVariousAgeGroups(string ageGroup)
    {
        // Arrange
        var course = new Course();

        // Act
        course.AgeGroup = ageGroup;

        // Assert
        Assert.Equal(ageGroup, course.AgeGroup);
    }

    [Fact]
    public void Course_Should_HandleLargeCapacity()
    {
        // Arrange
        var course = new Course();
        var largeCapacity = 1000;

        // Act
        course.MaxCapacity = largeCapacity;

        // Assert
        Assert.Equal(largeCapacity, course.MaxCapacity);
    }

    [Fact]
    public void Course_Should_HandleZeroCapacity()
    {
        // Arrange
        var course = new Course { MaxCapacity = 0 };

        // Act & Assert
        Assert.Equal(0, course.MaxCapacity);
        Assert.Equal(0, course.AvailableSpots);
        Assert.True(course.IsFull); // 0 enrollments in 0 capacity means "full" (can't enroll more)
    }
}
