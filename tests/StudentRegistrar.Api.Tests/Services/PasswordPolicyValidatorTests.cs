using Microsoft.Extensions.Logging;
using Moq;
using StudentRegistrar.Api.Services;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class PasswordPolicyValidatorTests
{
    private readonly Mock<IKeycloakService> _keycloakServiceMock;
    private readonly PasswordPolicyValidator _validator;

    public PasswordPolicyValidatorTests()
    {
        _keycloakServiceMock = new Mock<IKeycloakService>();
        _validator = new PasswordPolicyValidator(_keycloakServiceMock.Object, Mock.Of<ILogger<PasswordPolicyValidator>>());
    }

    [Fact]
    public async Task ValidateAsync_PolicyUnavailable_UsesBaselineAndRejectsWeakPassword()
    {
        _keycloakServiceMock.Setup(k => k.GetRealmPasswordPolicyAsync()).ReturnsAsync((string?)null);

        var result = await _validator.ValidateAsync("password");

        Assert.False(result.IsValid);
        Assert.True(result.UsedFallbackBaseline);
        Assert.NotEmpty(result.FailureReasons);
    }

    [Fact]
    public async Task ValidateAsync_PolicyUnavailable_AcceptsStrongPassword()
    {
        _keycloakServiceMock.Setup(k => k.GetRealmPasswordPolicyAsync()).ReturnsAsync((string?)null);

        var result = await _validator.ValidateAsync("Correct-Horse-Battery-99");

        Assert.True(result.IsValid);
        Assert.True(result.UsedFallbackBaseline);
    }

    [Fact]
    public async Task ValidateAsync_KeycloakThrows_FallsBackToBaseline()
    {
        _keycloakServiceMock.Setup(k => k.GetRealmPasswordPolicyAsync()).ThrowsAsync(new HttpRequestException("unreachable"));

        var result = await _validator.ValidateAsync("password");

        Assert.False(result.IsValid);
        Assert.True(result.UsedFallbackBaseline);
    }

    [Theory]
    [InlineData("blank")]
    [InlineData(null)]
    public async Task ValidateAsync_BlankPassword_AlwaysInvalid(string? password)
    {
        _keycloakServiceMock.Setup(k => k.GetRealmPasswordPolicyAsync())
            .ReturnsAsync("length(12) and upperCase(1) and lowerCase(1) and digits(1) and specialChars(1)");

        var result = await _validator.ValidateAsync(password == "blank" ? "" : password);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_KeycloakPolicy_RejectsPasswordFailingRules()
    {
        _keycloakServiceMock.Setup(k => k.GetRealmPasswordPolicyAsync())
            .ReturnsAsync("length(12) and upperCase(1) and lowerCase(1) and digits(1) and specialChars(1)");

        // Only lowercase + digits, no upper/special, and short.
        var result = await _validator.ValidateAsync("abc123");

        Assert.False(result.IsValid);
        Assert.False(result.UsedFallbackBaseline);
        Assert.Contains(result.FailureReasons, r => r.Contains("12 characters"));
        Assert.Contains(result.FailureReasons, r => r.Contains("uppercase"));
        Assert.Contains(result.FailureReasons, r => r.Contains("special"));
    }

    [Fact]
    public async Task ValidateAsync_KeycloakPolicy_AcceptsCompliantPassword()
    {
        _keycloakServiceMock.Setup(k => k.GetRealmPasswordPolicyAsync())
            .ReturnsAsync("length(12) and upperCase(1) and lowerCase(1) and digits(1) and specialChars(1)");

        var result = await _validator.ValidateAsync("Correct-Horse-99");

        Assert.True(result.IsValid);
        Assert.False(result.UsedFallbackBaseline);
    }
}
