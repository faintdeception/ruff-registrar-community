using StudentRegistrar.Models;
using System.Text.Json;
using Xunit;

namespace StudentRegistrar.Models.Tests;

public class EducatorTests
{
    [Fact]
    public void Educator_Should_HaveDefaultValues()
    {
        // Act
        var educator = new Educator();
        var now = DateTime.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, educator.Id);
        Assert.Equal(string.Empty, educator.FirstName);
        Assert.Equal(string.Empty, educator.LastName);
        Assert.Null(educator.Email);
        Assert.Null(educator.Phone);
        Assert.True(educator.IsActive);
        Assert.Equal("{}", educator.EducatorInfoJson);
        Assert.InRange(educator.CreatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.InRange(educator.UpdatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void FullName_Should_CombineFirstAndLastName()
    {
        // Arrange
        var educator = new Educator
        {
            FirstName = "Dr. Sarah",
            LastName = "Wilson"
        };

        // Act & Assert
        Assert.Equal("Dr. Sarah Wilson", educator.FullName);
    }

    [Fact]
    public void GetEducatorInfo_Should_ReturnValidInfo_WhenJsonIsValid()
    {
        // Arrange
        var educator = new Educator();
        var educatorInfo = new EducatorInfo
        {
            Bio = "Experienced mathematics teacher with 15 years in education",
            Qualifications = new List<string> { "M.Ed. Mathematics", "State Teaching Certificate", "AP Calculus Certified" },
            Specializations = new List<string> { "Advanced Mathematics", "Statistics", "Calculus" },
            Department = "Mathematics",
            CustomFields = new Dictionary<string, string> 
            { 
                { "Office", "Room 204" },
                { "OfficeHours", "Mon-Fri 3-4 PM" }
            }
        };
        educator.SetEducatorInfo(educatorInfo);

        // Act
        var retrievedInfo = educator.GetEducatorInfo();

        // Assert
        Assert.NotNull(retrievedInfo);
        Assert.Equal("Experienced mathematics teacher with 15 years in education", retrievedInfo.Bio);
        Assert.Equal(3, retrievedInfo.Qualifications.Count);
        Assert.Contains("M.Ed. Mathematics", retrievedInfo.Qualifications);
        Assert.Contains("State Teaching Certificate", retrievedInfo.Qualifications);
        Assert.Contains("AP Calculus Certified", retrievedInfo.Qualifications);
        Assert.Equal(3, retrievedInfo.Specializations.Count);
        Assert.Contains("Advanced Mathematics", retrievedInfo.Specializations);
        Assert.Contains("Statistics", retrievedInfo.Specializations);
        Assert.Equal("Mathematics", retrievedInfo.Department);
        Assert.Contains("Office", retrievedInfo.CustomFields.Keys);
        Assert.Equal("Room 204", retrievedInfo.CustomFields["Office"]);
        Assert.Contains("OfficeHours", retrievedInfo.CustomFields.Keys);
        Assert.Equal("Mon-Fri 3-4 PM", retrievedInfo.CustomFields["OfficeHours"]);
    }

    [Fact]
    public void GetEducatorInfo_Should_ReturnEmptyInfo_WhenJsonIsInvalid()
    {
        // Arrange
        var educator = new Educator
        {
            EducatorInfoJson = "invalid json"
        };

        // Act
        var info = educator.GetEducatorInfo();

        // Assert
        Assert.NotNull(info);
        Assert.Null(info.Bio);
        Assert.NotNull(info.Qualifications);
        Assert.Empty(info.Qualifications);
        Assert.NotNull(info.Specializations);
        Assert.Empty(info.Specializations);
        Assert.Null(info.Department);
        Assert.NotNull(info.CustomFields);
        Assert.Empty(info.CustomFields);
    }

    [Fact]
    public void SetEducatorInfo_Should_SerializeCorrectly()
    {
        // Arrange
        var educator = new Educator();
        var educatorInfo = new EducatorInfo
        {
            Bio = "Test bio",
            Qualifications = new List<string> { "Test Qualification" },
            Department = "Test Department"
        };

        // Act
        educator.SetEducatorInfo(educatorInfo);

        // Assert
        Assert.NotEqual("{}", educator.EducatorInfoJson);
        
        // Verify we can deserialize it back
        var deserializedInfo = JsonSerializer.Deserialize<EducatorInfo>(educator.EducatorInfoJson);
        Assert.NotNull(deserializedInfo);
        Assert.Equal("Test bio", deserializedInfo!.Bio);
        Assert.Contains("Test Qualification", deserializedInfo.Qualifications);
        Assert.Equal("Test Department", deserializedInfo.Department);
    }

    [Theory]
    [InlineData("", "", " ")]
    [InlineData("Sarah", "", "Sarah ")]
    [InlineData("", "Wilson", " Wilson")]
    [InlineData("Sarah", "Wilson", "Sarah Wilson")]
    [InlineData("Dr. John", "Smith", "Dr. John Smith")]
    public void FullName_Should_HandleVariousNameCombinations(string firstName, string lastName, string expected)
    {
        // Arrange
        var educator = new Educator
        {
            FirstName = firstName,
            LastName = lastName
        };

        // Act & Assert
        Assert.Equal(expected, educator.FullName);
    }

    [Fact]
    public void EducatorInfo_Should_InitializeEmptyCollections()
    {
        // Act
        var educatorInfo = new EducatorInfo();

        // Assert
        Assert.NotNull(educatorInfo.Qualifications);
        Assert.Empty(educatorInfo.Qualifications);
        Assert.NotNull(educatorInfo.Specializations);
        Assert.Empty(educatorInfo.Specializations);
        Assert.NotNull(educatorInfo.CustomFields);
        Assert.Empty(educatorInfo.CustomFields);
    }

    [Theory]
    [InlineData("teacher@school.edu")]
    [InlineData("educator.name@university.org")]
    [InlineData("first.last@district.k12.us")]
    public void Educator_Should_SupportValidEmailFormats(string email)
    {
        // Arrange
        var educator = new Educator();

        // Act
        educator.Email = email;

        // Assert
        Assert.Equal(email, educator.Email);
    }

    [Theory]
    [InlineData("(555) 123-4567")]
    [InlineData("555-123-4567")]
    [InlineData("555.123.4567")]
    [InlineData("+1-555-123-4567")]
    public void Educator_Should_SupportVariousPhoneFormats(string phone)
    {
        // Arrange
        var educator = new Educator();

        // Act
        educator.Phone = phone;

        // Assert
        Assert.Equal(phone, educator.Phone);
    }

    [Fact]
    public void Educator_Should_SupportActiveInactiveStates()
    {
        // Arrange
        var educator = new Educator();

        // Default should be active
        Assert.True(educator.IsActive);

        // Act - deactivate
        educator.IsActive = false;

        // Assert
        Assert.False(educator.IsActive);

        // Act - reactivate
        educator.IsActive = true;

        // Assert
        Assert.True(educator.IsActive);
    }

    [Fact]
    public void Educator_Should_HandleIndependentEducatorScenario()
    {
        // Arrange - Educator profile independent of course assignment
        var educator = new Educator
        {
            FirstName = "Independent",
            LastName = "Teacher",
            Email = "independent@freelance.edu",
            IsActive = true
        };

        // Assert
        Assert.True(educator.IsActive);
        Assert.Equal("Independent Teacher", educator.FullName);
    }

    [Fact]
    public void Educator_Should_HandleComplexEducatorInfo()
    {
        // Arrange
        var educator = new Educator();
        var complexInfo = new EducatorInfo
        {
            Bio = "Dr. Johnson has been teaching for over 20 years and specializes in advanced mathematics. She holds a PhD in Mathematics Education and has published numerous papers on pedagogical approaches to calculus instruction.",
            Qualifications = new List<string> 
            { 
                "PhD Mathematics Education - Stanford University",
                "M.S. Mathematics - MIT",
                "B.S. Mathematics - UC Berkeley",
                "California Teaching Credential - Secondary Mathematics",
                "AP Calculus AB/BC Certification",
                "National Board Certification - Mathematics"
            },
            Specializations = new List<string> 
            { 
                "Calculus AB/BC", 
                "Statistics", 
                "Pre-Calculus", 
                "Algebra II", 
                "Mathematics Education Research",
                "STEM Curriculum Development"
            },
            Department = "Mathematics & Computer Science",
            CustomFields = new Dictionary<string, string>
            {
                { "Office", "Science Building Room 301" },
                { "OfficeHours", "Monday/Wednesday 2-4 PM, Tuesday/Thursday 10-11 AM" },
                { "PreferredPronouns", "She/Her" },
                { "YearsExperience", "22" },
                { "ResearchInterests", "Mathematics anxiety, visual learning in calculus" },
                { "Publications", "15 peer-reviewed articles" }
            }
        };

        // Act
        educator.SetEducatorInfo(complexInfo);
        var retrievedInfo = educator.GetEducatorInfo();

        // Assert
        Assert.NotNull(retrievedInfo);
        Assert.Equal(6, retrievedInfo.Qualifications.Count);
        Assert.Equal(6, retrievedInfo.Specializations.Count);
        Assert.Equal(6, retrievedInfo.CustomFields.Count);
        Assert.Equal("22", retrievedInfo.CustomFields["YearsExperience"]);
        Assert.Equal("Mathematics anxiety, visual learning in calculus", retrievedInfo.CustomFields["ResearchInterests"]);
    }
}
