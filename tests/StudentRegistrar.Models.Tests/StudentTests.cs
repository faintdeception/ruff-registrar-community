using StudentRegistrar.Models;
using System.Text.Json;
using Xunit;

namespace StudentRegistrar.Models.Tests;

public class StudentTests
{
    [Fact]
    public void Student_Should_HaveDefaultValues()
    {
        // Act
        var student = new Student();
        var now = DateTime.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.Equal(Guid.Empty, student.AccountHolderId);
        Assert.Equal(string.Empty, student.FirstName);
        Assert.Equal(string.Empty, student.LastName);
        Assert.Null(student.Grade);
        Assert.Null(student.DateOfBirth);
        Assert.Equal("{}", student.StudentInfoJson);
        Assert.Null(student.Notes);
        Assert.InRange(student.CreatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.InRange(student.UpdatedAt, now - TimeSpan.FromSeconds(1), now + TimeSpan.FromSeconds(1));
        Assert.NotNull(student.Enrollments);
        Assert.Empty(student.Enrollments);
    }

    [Fact]
    public void FullName_Should_CombineFirstAndLastName()
    {
        // Arrange
        var student = new Student
        {
            FirstName = "Alice",
            LastName = "Johnson"
        };

        // Act & Assert
        Assert.Equal("Alice Johnson", student.FullName);
    }

    [Fact]
    public void GetStudentInfo_Should_ReturnValidInfo_WhenJsonIsValid()
    {
        // Arrange
        var student = new Student();
        var studentInfo = new StudentInfo
        {
            SpecialConditions = new List<string> { "ADHD", "Needs extra time" },
            LearningDisabilities = new List<string> { "Dyslexia" },
            Allergies = new List<string> { "Peanuts", "Shellfish" },
            Medications = new List<string> { "Inhaler" },
            PreferredName = "Al",
            ParentNotes = "Please call if issues arise",
            TeacherNotes = "Very bright student"
        };
        student.SetStudentInfo(studentInfo);

        // Act
        var retrievedInfo = student.GetStudentInfo();

        // Assert
        Assert.NotNull(retrievedInfo);
        Assert.Equal(2, retrievedInfo.SpecialConditions.Count);
        Assert.Contains("ADHD", retrievedInfo.SpecialConditions);
        Assert.Contains("Needs extra time", retrievedInfo.SpecialConditions);
        Assert.Contains("Dyslexia", retrievedInfo.LearningDisabilities);
        Assert.Equal(2, retrievedInfo.Allergies.Count);
        Assert.Contains("Peanuts", retrievedInfo.Allergies);
        Assert.Contains("Shellfish", retrievedInfo.Allergies);
        Assert.Contains("Inhaler", retrievedInfo.Medications);
        Assert.Equal("Al", retrievedInfo.PreferredName);
        Assert.Equal("Please call if issues arise", retrievedInfo.ParentNotes);
        Assert.Equal("Very bright student", retrievedInfo.TeacherNotes);
    }

    [Fact]
    public void GetStudentInfo_Should_ReturnEmptyInfo_WhenJsonIsInvalid()
    {
        // Arrange
        var student = new Student
        {
            StudentInfoJson = "invalid json"
        };

        // Act
        var info = student.GetStudentInfo();

        // Assert
        Assert.NotNull(info);
        Assert.NotNull(info.SpecialConditions);
        Assert.Empty(info.SpecialConditions);
        Assert.NotNull(info.LearningDisabilities);
        Assert.Empty(info.LearningDisabilities);
        Assert.NotNull(info.Allergies);
        Assert.Empty(info.Allergies);
        Assert.NotNull(info.Medications);
        Assert.Empty(info.Medications);
        Assert.Null(info.PreferredName);
        Assert.Null(info.ParentNotes);
        Assert.Null(info.TeacherNotes);
    }

    [Fact]
    public void SetStudentInfo_Should_SerializeCorrectly()
    {
        // Arrange
        var student = new Student();
        var studentInfo = new StudentInfo
        {
            SpecialConditions = new List<string> { "Test condition" },
            PreferredName = "TestName"
        };

        // Act
        student.SetStudentInfo(studentInfo);

        // Assert
        Assert.NotEqual("{}", student.StudentInfoJson);
        
        // Verify we can deserialize it back
        var deserializedInfo = JsonSerializer.Deserialize<StudentInfo>(student.StudentInfoJson);
        Assert.NotNull(deserializedInfo);
        Assert.Contains("Test condition", deserializedInfo!.SpecialConditions);
        Assert.Equal("TestName", deserializedInfo.PreferredName);
    }

    [Theory]
    [InlineData("", "", " ")]
    [InlineData("Alice", "", "Alice ")]
    [InlineData("", "Johnson", " Johnson")]
    [InlineData("Alice", "Johnson", "Alice Johnson")]
    public void FullName_Should_HandleVariousNameCombinations(string firstName, string lastName, string expected)
    {
        // Arrange
        var student = new Student
        {
            FirstName = firstName,
            LastName = lastName
        };

        // Act & Assert
        Assert.Equal(expected, student.FullName);
    }

    [Fact]
    public void Student_Should_AllowValidGrades()
    {
        // Arrange
        var student = new Student();
        var validGrades = new[] { "K", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };

        // Act & Assert
        foreach (var grade in validGrades)
        {
            student.Grade = grade;
            Assert.Equal(grade, student.Grade);
        }
    }

    [Fact]
    public void StudentInfo_Should_InitializeEmptyCollections()
    {
        // Act
        var studentInfo = new StudentInfo();

        // Assert
        Assert.NotNull(studentInfo.SpecialConditions);
        Assert.Empty(studentInfo.SpecialConditions);
        Assert.NotNull(studentInfo.LearningDisabilities);
        Assert.Empty(studentInfo.LearningDisabilities);
        Assert.NotNull(studentInfo.Allergies);
        Assert.Empty(studentInfo.Allergies);
        Assert.NotNull(studentInfo.Medications);
        Assert.Empty(studentInfo.Medications);
    }

    [Fact]
    public void Student_Should_SupportDateOfBirth()
    {
        // Arrange
        var student = new Student();
        var birthDate = new DateTime(2010, 5, 15);

        // Act
        student.DateOfBirth = birthDate;

        // Assert
        Assert.Equal(birthDate, student.DateOfBirth);
    }

    [Fact]
    public void Student_Should_SupportNotes()
    {
        // Arrange
        var student = new Student();
        var notes = "This student excels in mathematics and enjoys reading.";

        // Act
        student.Notes = notes;

        // Assert
        Assert.Equal(notes, student.Notes);
    }

    [Fact]
    public void Student_Should_RequireAccountHolderId()
    {
        // Arrange
        var student = new Student();
        var accountHolderId = Guid.NewGuid();

        // Act
        student.AccountHolderId = accountHolderId;

        // Assert
        Assert.Equal(accountHolderId, student.AccountHolderId);
        Assert.NotEqual(Guid.Empty, student.AccountHolderId);
    }
}
