using Microsoft.Extensions.Logging;
using Moq;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class PasswordServiceTests
{
    private readonly Mock<ILogger<PasswordService>> _loggerMock;
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _loggerMock = new Mock<ILogger<PasswordService>>();
        _passwordService = new PasswordService(_loggerMock.Object);
    }

    [Fact]
    public void GenerateSecurePassword_DefaultLength_ReturnsValidPassword()
    {
        // Act
        var password = _passwordService.GenerateSecurePassword();

        // Assert
        Assert.NotNull(password);
        Assert.Equal(14, password.Length);
        Assert.True(_passwordService.ValidatePasswordComplexity(password));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(20)]
    public void GenerateSecurePassword_VariousLengths_ReturnsCorrectLength(int length)
    {
        // Act
        var password = _passwordService.GenerateSecurePassword(length);

        // Assert
        Assert.NotNull(password);
        Assert.Equal(length, password.Length);
        Assert.True(_passwordService.ValidatePasswordComplexity(password));
    }

    [Fact]
    public void GenerateSecurePassword_TooShort_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.GenerateSecurePassword(7));
    }

    [Fact]
    public void ValidatePasswordComplexity_ValidPassword_ReturnsTrue()
    {
        // Arrange
        var validPassword = "Abc123!@#";

        // Act
        var result = _passwordService.ValidatePasswordComplexity(validPassword);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("Abc")]
    [InlineData("Abc123")]
    [InlineData("abc123!")]
    [InlineData("ABC123!")]
    [InlineData("Abc!@#")]
    public void ValidatePasswordComplexity_InvalidPasswords_ReturnsFalse(string password)
    {
        // Act
        var result = _passwordService.ValidatePasswordComplexity(password);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", PasswordStrength.VeryWeak)]
    [InlineData("abc", PasswordStrength.VeryWeak)]
    [InlineData("Abc123!", PasswordStrength.Good)]
    [InlineData("Abc123!@#$", PasswordStrength.Strong)]
    [InlineData("Abc123!@#$%^&*", PasswordStrength.VeryStrong)]
    [InlineData("Abc123!@#$%^&*()", PasswordStrength.VeryStrong)]
    public void AssessPasswordStrength_VariousPasswords_ReturnsExpectedStrength(string password, PasswordStrength expectedStrength)
    {
        // Act
        var strength = _passwordService.AssessPasswordStrength(password);

        // Assert
        Assert.Equal(expectedStrength, strength);
    }

    [Fact]
    public void GenerateSecurePassword_MultipleGenerations_ProducesDifferentPasswords()
    {
        // Act
        var password1 = _passwordService.GenerateSecurePassword();
        var password2 = _passwordService.GenerateSecurePassword();
        var password3 = _passwordService.GenerateSecurePassword();

        // Assert
        Assert.NotEqual(password1, password2);
        Assert.NotEqual(password2, password3);
        Assert.NotEqual(password1, password3);
    }

    [Fact]
    public void GenerateSecurePassword_ContainsAllCharacterTypes()
    {
        // Act
        var password = _passwordService.GenerateSecurePassword(20); // Longer password to increase probability

        // Assert
        Assert.Contains(password, c => char.IsUpper(c));
        Assert.Contains(password, c => char.IsLower(c));
        Assert.Contains(password, c => char.IsDigit(c));
        Assert.Contains(password, c => "!@#$%&*+=?".Contains(c));
    }

    [Fact]
    public void GenerateSecurePassword_DoesNotContainAmbiguousCharacters()
    {
        // Arrange
        var ambiguousChars = "0O1lIi";

        // Act
        var password = _passwordService.GenerateSecurePassword(50); // Longer password to test thoroughly

        // Assert
        foreach (var ambiguous in ambiguousChars)
        {
            Assert.DoesNotContain(ambiguous, password);
        }
    }

    [Fact]
    public void GenerateSecurePassword_ConsistentlyPassesComplexityValidation()
    {
        // Act & Assert - Test multiple generations
        for (int i = 0; i < 100; i++)
        {
            var password = _passwordService.GenerateSecurePassword();
            Assert.True(_passwordService.ValidatePasswordComplexity(password), 
                $"Generated password failed complexity validation: {password}");
        }
    }
}
