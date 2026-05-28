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
        var now = DateTime.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, semester.Id);
        Assert.Equal(string.Empty, semester.Name);
        Assert.Equal(string.Empty, semester.Code);
        Assert.Equal(default, semester.StartDate);
        Assert.Equal(default, semester.EndDate);
        Assert.Equal(default, semester.RegistrationStartDate);
        Assert.Equal(default, semester.RegistrationEndDate);
        Assert.True(semester.IsActive);
        Assert.Equal("{}", semester.PeriodConfigJson);
        Assert.InRange(semester.CreatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.InRange(semester.UpdatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.NotNull(semester.Courses);
        Assert.Empty(semester.Courses);
        Assert.NotNull(semester.Enrollments);
        Assert.Empty(semester.Enrollments);
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
        Assert.True(semester.IsRegistrationOpen);
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
        Assert.False(semester.IsRegistrationOpen);
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
        Assert.False(semester.IsRegistrationOpen);
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
        Assert.False(semester.IsRegistrationOpen);
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
        Assert.NotNull(retrievedConfig);
        Assert.Equal(2, retrievedConfig.Periods.Count);
        
        var period1 = retrievedConfig.Periods.First();
        Assert.Equal("Period 1", period1.Name);
        Assert.Equal("P1", period1.Code);
        Assert.True(period1.IsActive);
        Assert.Equal("First period of semester", period1.Description);
        
        Assert.Equal(2, retrievedConfig.Holidays.Count);
        var laborDay = retrievedConfig.Holidays.First();
        Assert.Equal("Labor Day", laborDay.Name);
        Assert.Equal(new DateTime(2024, 9, 2), laborDay.Date);
        Assert.Equal("Federal holiday", laborDay.Description);
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
        Assert.NotNull(config);
        Assert.NotNull(config.Periods);
        Assert.Empty(config.Periods);
        Assert.NotNull(config.Holidays);
        Assert.Empty(config.Holidays);
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
        Assert.NotEqual("{}", semester.PeriodConfigJson);
        
        // Verify we can deserialize it back
        var deserializedConfig = JsonSerializer.Deserialize<PeriodConfiguration>(semester.PeriodConfigJson);
        Assert.NotNull(deserializedConfig);
        Assert.Single(deserializedConfig!.Periods);
        Assert.Equal("Test Period", deserializedConfig.Periods.First().Name);
        Assert.Single(deserializedConfig.Holidays);
        Assert.Equal("Test Holiday", deserializedConfig.Holidays.First().Name);
    }

    [Fact]
    public void PeriodConfiguration_Should_InitializeEmptyCollections()
    {
        // Act
        var config = new PeriodConfiguration();

        // Assert
        Assert.NotNull(config.Periods);
        Assert.Empty(config.Periods);
        Assert.NotNull(config.Holidays);
        Assert.Empty(config.Holidays);
    }

    [Fact]
    public void Period_Should_HaveDefaultValues()
    {
        // Act
        var period = new Period();

        // Assert
        Assert.Equal(string.Empty, period.Name);
        Assert.Equal(string.Empty, period.Code);
        Assert.Equal(default, period.StartDate);
        Assert.Equal(default, period.EndDate);
        Assert.True(period.IsActive);
        Assert.Null(period.Description);
    }

    [Fact]
    public void Holiday_Should_HaveDefaultValues()
    {
        // Act
        var holiday = new Holiday();

        // Assert
        Assert.Equal(string.Empty, holiday.Name);
        Assert.Equal(default, holiday.Date);
        Assert.Null(holiday.Description);
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
        Assert.Equal(semesterName, semester.Name);
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
        Assert.Equal(semesterCode, semester.Code);
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
        Assert.Equal(new DateTime(2024, 9, 1), semester.StartDate);
        Assert.Equal(new DateTime(2024, 12, 15), semester.EndDate);
        Assert.Equal(new DateTime(2024, 8, 1), semester.RegistrationStartDate);
        Assert.Equal(new DateTime(2024, 8, 31), semester.RegistrationEndDate);
        
        // Registration should typically end before semester starts
        Assert.True(semester.RegistrationEndDate < semester.StartDate);
    }

    [Fact]
    public void Semester_Should_HandleActiveInactiveState()
    {
        // Arrange
        var semester = new Semester();

        // Default should be active
        Assert.True(semester.IsActive);

        // Act - deactivate
        semester.IsActive = false;

        // Assert
        Assert.False(semester.IsActive);
    }
}
