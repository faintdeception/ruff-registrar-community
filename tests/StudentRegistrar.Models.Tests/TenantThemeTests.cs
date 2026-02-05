using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Models.Tests;

/// <summary>
/// Tests for TenantTheme class, focusing on security validation of custom CSS.
/// </summary>
public class TenantThemeTests
{
    [Fact]
    public void GetSanitizedCustomCss_WithNullCss_ReturnsEmptyString()
    {
        // Arrange
        var theme = new TenantTheme
        {
            CustomCss = null
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithEmptyCss_ReturnsEmptyString()
    {
        // Arrange
        var theme = new TenantTheme
        {
            CustomCss = ""
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithWhitespaceCss_ReturnsEmptyString()
    {
        // Arrange
        var theme = new TenantTheme
        {
            CustomCss = "   \t\n  "
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithValidCss_ReturnsSanitizedCss()
    {
        // Arrange
        var validCss = ".my-class { color: red; background: blue; }";
        var theme = new TenantTheme
        {
            CustomCss = validCss
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.Equal(validCss, result);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithScriptTags_RemovesScriptTags()
    {
        // Arrange
        var maliciousCss = ".my-class { color: red; } <script>alert('XSS')</script>";
        var theme = new TenantTheme
        {
            CustomCss = maliciousCss
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.DoesNotContain("<script>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result);
        Assert.Contains(".my-class", result);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithJavaScriptUrl_RemovesJavaScriptUrl()
    {
        // Arrange
        var maliciousCss = ".my-class { background: url(javascript:alert('XSS')); }";
        var theme = new TenantTheme
        {
            CustomCss = maliciousCss
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".my-class", result);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithExpression_RemovesExpression()
    {
        // Arrange
        var maliciousCss = ".my-class { width: expression(alert('XSS')); }";
        var theme = new TenantTheme
        {
            CustomCss = maliciousCss
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.DoesNotContain("expression(", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".my-class", result);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithImport_RemovesImport()
    {
        // Arrange
        var maliciousCss = "@import url('http://evil.com/malicious.css'); .my-class { color: red; }";
        var theme = new TenantTheme
        {
            CustomCss = maliciousCss
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.DoesNotContain("@import", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".my-class", result);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithMultipleThreats_RemovesAllThreats()
    {
        // Arrange
        var maliciousCss = @"
            <script>alert('XSS')</script>
            .my-class { 
                color: red;
                background: url(javascript:alert('XSS'));
                width: expression(alert('XSS'));
            }
            @import url('http://evil.com/malicious.css');
            .safe-class { padding: 10px; }
        ";
        var theme = new TenantTheme
        {
            CustomCss = maliciousCss
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.DoesNotContain("<script>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expression(", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@import", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".my-class", result);
        Assert.Contains(".safe-class", result);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithVeryLongInput_TruncatesInput()
    {
        // Arrange
        var longCss = new string('a', 60000); // Exceeds 50000 character limit
        var theme = new TenantTheme
        {
            CustomCss = longCss
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.True(result.Length <= 50000, $"Expected length <= 50000, but got {result.Length}");
    }

    [Fact]
    public void SanitizeCustomCss_StaticMethod_WorksCorrectly()
    {
        // Arrange
        var maliciousCss = ".my-class { color: red; } <script>alert('XSS')</script>";

        // Act
        var result = TenantTheme.SanitizeCustomCss(maliciousCss);

        // Assert
        Assert.DoesNotContain("<script>", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".my-class", result);
    }

    [Fact]
    public void CustomCssProperty_CanBeSetAndRetrieved()
    {
        // Arrange
        var theme = new TenantTheme();
        var css = ".test { color: blue; }";

        // Act
        theme.CustomCss = css;

        // Assert - Raw property returns unsanitized value
        Assert.Equal(css, theme.CustomCss);
    }

    [Fact]
    public void CustomCssRaw_IsUsedForSerialization()
    {
        // Arrange
        var theme = new TenantTheme
        {
            CustomCss = ".test { color: blue; }"
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(theme);

        // Assert
        Assert.Contains("customCss", json);
        Assert.Contains(".test", json);
    }

    [Fact]
    public void GetSanitizedCustomCss_AlwaysReturnsSafeValue()
    {
        // Arrange
        var theme = new TenantTheme
        {
            CustomCss = "<script>alert('XSS')</script>.safe { color: red; }"
        };

        // Act
        var sanitized1 = theme.GetSanitizedCustomCss();
        var sanitized2 = theme.GetSanitizedCustomCss();

        // Assert - Multiple calls return the same sanitized result
        Assert.Equal(sanitized1, sanitized2);
        Assert.DoesNotContain("<script>", sanitized1, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetSanitizedCustomCss_WithCaseVariations_RemovesThreatsCaseInsensitive()
    {
        // Arrange
        var maliciousCss = @"
            <ScRiPt>alert('XSS')</ScRiPt>
            .my-class { 
                background: url(JaVaScRiPt:alert('XSS'));
                width: ExPrEsSiOn(alert('XSS'));
            }
            @ImPoRt url('http://evil.com/malicious.css');
        ";
        var theme = new TenantTheme
        {
            CustomCss = maliciousCss
        };

        // Act
        var result = theme.GetSanitizedCustomCss();

        // Assert
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expression", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("import", result, StringComparison.OrdinalIgnoreCase);
    }
}
