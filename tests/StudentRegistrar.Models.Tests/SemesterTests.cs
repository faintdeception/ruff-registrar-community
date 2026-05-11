using FluentAssertions;
using StudentRegistrar.Models;
using System.Text.Json;
using Xunit;

namespace StudentRegistrar.Models.Tests;

public class SemesterTests
{
    [Fact]
    public void Semester_Should_HaveDefaultValues()
    {
        // Act
        var semester = new Semester();

        // Assert
        semester.Id.Should().NotBeEmpty();
        semester.Name.Should().BeEmpty();
        semester.Code.Should().BeNull();
        semester.StartDate.Should().Be(default);
        semester.EndDate.Should().Be(default);
        semester.RegistrationStartDate.Should().BeNull();
        semester.RegistrationEndDate.Should().BeNull();
        semester.IsActive.Should().BeTrue();
        semester.PeriodConfigJson.Should().Be("{}");
        semester.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        semester.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        semester.Courses.Should().NotBeNull().And.BeEmpty();
        semester.Enrollments.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void IsRegistrationOpen_Should_ReturnTrue_WhenWithinRegistrationPeriod()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var semester = new Semester
        {
            RegistrationStartDate = now.AddDays(-5),
            RegistrationEndDate = now.AddDays(5)
        };

        // Act & Assert
        semester.IsRegistrationOpen.Should().BeTrue();
    }

    [Fact]
    public void IsRegistrationOpen_Should_ReturnFalse_WhenBeforeRegistrationPeriod()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var semester = new Semester
        {
            RegistrationStartDate = now.AddDays(1),
            RegistrationEndDate = now.AddDays(10)
        };

        // Act & Assert
        semester.IsRegistrationOpen.Should().BeFalse();
    }

    [Fact]
    public void IsRegistrationOpen_Should_ReturnFalse_WhenAfterRegistrationPeriod()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var semester = new Semester
        {
            RegistrationStartDate = now.AddDays(-10),
            RegistrationEndDate = now.AddDays(-1)
        };

        // Act & Assert
        semester.IsRegistrationOpen.Should().BeFalse();
    }

    [Fact]
    public void IsRegistrationOpen_Should_ReturnFalse_WhenRegistrationEndsNow()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var semester = new Semester
        {
            RegistrationStartDate = now.AddDays(-5),
            RegistrationEndDate = now // Exactly now - should be closed
        };

        // Act & Assert
        semester.IsRegistrationOpen.Should().BeFalse();
    }

    [Fact]
    public void IsRegistrationOpen_Should_ReturnFalse_WhenRegistrationDatesAreMissing()
    {
        var semester = new Semester();

        semester.IsRegistrationOpen.Should().BeFalse();
    }

    [Fact]
    public void GetPeriodConfiguration_Should_ReturnValidConfig_WhenJsonIsValid()
    {
        // Arrange
        var semester = new Semester();
        var config = new PeriodConfiguration
        {
            Periods = new List<Period>
            {
                new() 
                { 
                    Name = "Period 1", 
                    Code = "P1", 
                    StartDate = DateTime.Today, 
                    EndDate = DateTime.Today.AddDays(30),
                    IsActive = true,
                    Description = "First period of semester"
                },
                new() 
                { 
                    Name = "Period 2", 
                    Code = "P2", 
                    StartDate = DateTime.Today.AddDays(31), 
                    EndDate = DateTime.Today.AddDays(60),
                    IsActive = true
                }
            },
            Holidays = new List<Holiday>
            {
                new() 
                { 
                    Name = "Labor Day", 
                    Date = new DateTime(2024, 9, 2),
                    Description = "Federal holiday"
                },
                new() 
                { 
                    Name = "Thanksgiving", 
                    Date = new DateTime(2024, 11, 28)
                }
            }
        };
        semester.SetPeriodConfiguration(config);

        // Act
        var retrievedConfig = semester.GetPeriodConfiguration();

        // Assert
        retrievedConfig.Should().NotBeNull();
        retrievedConfig.Periods.Should().HaveCount(2);
        
        var period1 = retrievedConfig.Periods.First();
        period1.Name.Should().Be("Period 1");
        period1.Code.Should().Be("P1");
        period1.IsActive.Should().BeTrue();
        period1.Description.Should().Be("First period of semester");
        
        retrievedConfig.Holidays.Should().HaveCount(2);
        var laborDay = retrievedConfig.Holidays.First();
        laborDay.Name.Should().Be("Labor Day");
        laborDay.Date.Should().Be(new DateTime(2024, 9, 2));
        laborDay.Description.Should().Be("Federal holiday");
    }

    [Fact]
    public void GetPeriodConfiguration_Should_ReturnEmptyConfig_WhenJsonIsInvalid()
    {
        // Arrange
        var semester = new Semester
        {
            PeriodConfigJson = "invalid json"
        };

        // Act
        var config = semester.GetPeriodConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.Periods.Should().NotBeNull().And.BeEmpty();
        config.Holidays.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void SetPeriodConfiguration_Should_SerializeCorrectly()
    {
        // Arrange
        var semester = new Semester();
        var config = new PeriodConfiguration
        {
            Periods = new List<Period>
            {
                new() { Name = "Test Period", Code = "TP" }
            },
            Holidays = new List<Holiday>
            {
                new() { Name = "Test Holiday", Date = DateTime.Today }
            }
        };

        // Act
        semester.SetPeriodConfiguration(config);

        // Assert
        semester.PeriodConfigJson.Should().NotBe("{}");
        
        // Verify we can deserialize it back
        var deserializedConfig = JsonSerializer.Deserialize<PeriodConfiguration>(semester.PeriodConfigJson);
        deserializedConfig.Should().NotBeNull();
        deserializedConfig!.Periods.Should().HaveCount(1);
        deserializedConfig.Periods.First().Name.Should().Be("Test Period");
        deserializedConfig.Holidays.Should().HaveCount(1);
        deserializedConfig.Holidays.First().Name.Should().Be("Test Holiday");
    }

    [Fact]
    public void PeriodConfiguration_Should_InitializeEmptyCollections()
    {
        // Act
        var config = new PeriodConfiguration();

        // Assert
        config.Periods.Should().NotBeNull().And.BeEmpty();
        config.Holidays.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Period_Should_HaveDefaultValues()
    {
        // Act
        var period = new Period();

        // Assert
        period.Name.Should().BeEmpty();
        period.Code.Should().BeEmpty();
        period.StartDate.Should().Be(default);
        period.EndDate.Should().Be(default);
        period.IsActive.Should().BeTrue();
        period.Description.Should().BeNull();
    }

    [Fact]
    public void Holiday_Should_HaveDefaultValues()
    {
        // Act
        var holiday = new Holiday();

        // Assert
        holiday.Name.Should().BeEmpty();
        holiday.Date.Should().Be(default);
        holiday.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("Fall 2024")]
    [InlineData("Spring 2025")]
    [InlineData("Summer 2024")]
    [InlineData("Winter Break 2024")]
    public void Semester_Should_SupportVariousNames(string semesterName)
    {
        // Arrange
        var semester = new Semester();

        // Act
        semester.Name = semesterName;

        // Assert
        semester.Name.Should().Be(semesterName);
    }

    [Theory]
    [InlineData("F24")]
    [InlineData("SP25")]
    [InlineData("SU24")]
    [InlineData("W24")]
    public void Semester_Should_SupportVariousCodes(string semesterCode)
    {
        // Arrange
        var semester = new Semester();

        // Act
        semester.Code = semesterCode;

        // Assert
        semester.Code.Should().Be(semesterCode);
    }

    [Fact]
    public void Semester_Should_ValidateDateRanges()
    {
        // Arrange
        var semester = new Semester
        {
            StartDate = new DateTime(2024, 9, 1),
            EndDate = new DateTime(2024, 12, 15),
            RegistrationStartDate = new DateTime(2024, 8, 1),
            RegistrationEndDate = new DateTime(2024, 8, 31)
        };

        // Assert - Basic validation that dates are set correctly
        semester.StartDate.Should().Be(new DateTime(2024, 9, 1));
        semester.EndDate.Should().Be(new DateTime(2024, 12, 15));
        semester.RegistrationStartDate.Should().Be(new DateTime(2024, 8, 1));
        semester.RegistrationEndDate.Should().Be(new DateTime(2024, 8, 31));
        
        // Registration should typically end before semester starts
        semester.RegistrationEndDate.Should().BeBefore(semester.StartDate);
    }

    [Fact]
    public void Semester_Should_HandleActiveInactiveState()
    {
        // Arrange
        var semester = new Semester();

        // Default should be active
        semester.IsActive.Should().BeTrue();

        // Act - deactivate
        semester.IsActive = false;

        // Assert
        semester.IsActive.Should().BeFalse();
    }
}
