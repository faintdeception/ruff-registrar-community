using FluentAssertions;
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

        // Assert
        educator.Id.Should().NotBeEmpty();
        educator.FirstName.Should().BeEmpty();
        educator.LastName.Should().BeEmpty();
        educator.Email.Should().BeNull();
        educator.Phone.Should().BeNull();
        educator.IsActive.Should().BeTrue();
        educator.EducatorInfoJson.Should().Be("{}");
        educator.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        educator.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
        educator.FullName.Should().Be("Dr. Sarah Wilson");
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
        retrievedInfo.Should().NotBeNull();
        retrievedInfo.Bio.Should().Be("Experienced mathematics teacher with 15 years in education");
        retrievedInfo.Qualifications.Should().HaveCount(3);
        retrievedInfo.Qualifications.Should().Contain("M.Ed. Mathematics");
        retrievedInfo.Qualifications.Should().Contain("State Teaching Certificate");
        retrievedInfo.Qualifications.Should().Contain("AP Calculus Certified");
        retrievedInfo.Specializations.Should().HaveCount(3);
        retrievedInfo.Specializations.Should().Contain("Advanced Mathematics");
        retrievedInfo.Specializations.Should().Contain("Statistics");
        retrievedInfo.Department.Should().Be("Mathematics");
        retrievedInfo.CustomFields.Should().ContainKey("Office");
        retrievedInfo.CustomFields["Office"].Should().Be("Room 204");
        retrievedInfo.CustomFields.Should().ContainKey("OfficeHours");
        retrievedInfo.CustomFields["OfficeHours"].Should().Be("Mon-Fri 3-4 PM");
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
        info.Should().NotBeNull();
        info.Bio.Should().BeNull();
        info.Qualifications.Should().NotBeNull().And.BeEmpty();
        info.Specializations.Should().NotBeNull().And.BeEmpty();
        info.Department.Should().BeNull();
        info.CustomFields.Should().NotBeNull().And.BeEmpty();
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
        educator.EducatorInfoJson.Should().NotBe("{}");
        
        // Verify we can deserialize it back
        var deserializedInfo = JsonSerializer.Deserialize<EducatorInfo>(educator.EducatorInfoJson);
        deserializedInfo.Should().NotBeNull();
        deserializedInfo!.Bio.Should().Be("Test bio");
        deserializedInfo.Qualifications.Should().Contain("Test Qualification");
        deserializedInfo.Department.Should().Be("Test Department");
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
        educator.FullName.Should().Be(expected);
    }

    [Fact]
    public void EducatorInfo_Should_InitializeEmptyCollections()
    {
        // Act
        var educatorInfo = new EducatorInfo();

        // Assert
        educatorInfo.Qualifications.Should().NotBeNull().And.BeEmpty();
        educatorInfo.Specializations.Should().NotBeNull().And.BeEmpty();
        educatorInfo.CustomFields.Should().NotBeNull().And.BeEmpty();
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
        educator.Email.Should().Be(email);
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
        educator.Phone.Should().Be(phone);
    }

    [Fact]
    public void Educator_Should_SupportActiveInactiveStates()
    {
        // Arrange
        var educator = new Educator();

        // Default should be active
        educator.IsActive.Should().BeTrue();

        // Act - deactivate
        educator.IsActive = false;

        // Assert
        educator.IsActive.Should().BeFalse();

        // Act - reactivate
        educator.IsActive = true;

        // Assert
        educator.IsActive.Should().BeTrue();
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
        educator.IsActive.Should().BeTrue();
        educator.FullName.Should().Be("Independent Teacher");
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
        retrievedInfo.Should().NotBeNull();
        retrievedInfo.Qualifications.Should().HaveCount(6);
        retrievedInfo.Specializations.Should().HaveCount(6);
        retrievedInfo.CustomFields.Should().HaveCount(6);
        retrievedInfo.CustomFields["YearsExperience"].Should().Be("22");
        retrievedInfo.CustomFields["ResearchInterests"].Should().Be("Mathematics anxiety, visual learning in calculus");
    }
}
