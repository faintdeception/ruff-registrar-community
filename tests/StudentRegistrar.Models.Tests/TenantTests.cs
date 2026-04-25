using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Models.Tests;

/// <summary>
/// Tests for Tenant class, focusing on theme configuration management.
/// </summary>
public class TenantTests
{
    [Fact]
    public void GetTheme_WithDefaultJson_ReturnsDefaultTheme()
    {
        // Arrange
        var tenant = new Tenant
        {
            ThemeConfigJson = "{}"
        };

        // Act
        var theme = tenant.GetTheme();

        // Assert
        Assert.NotNull(theme);
        Assert.Equal("#3B82F6", theme.PrimaryColor);
        Assert.Equal("#10B981", theme.SecondaryColor);
    }

    [Fact]
    public void SetTheme_WithCustomCss_PreservesCustomCss()
    {
        // Arrange
        var tenant = new Tenant();
        var theme = new TenantTheme
        {
            PrimaryColor = "#FF0000",
            CustomCss = ".test { color: red; }"
        };

        // Act
        tenant.SetTheme(theme);
        var retrievedTheme = tenant.GetTheme();

        // Assert
        Assert.Equal("#FF0000", retrievedTheme.PrimaryColor);
        Assert.Equal(".test { color: red; }", retrievedTheme.CustomCss);
    }

    [Fact]
    public void GetTheme_AfterSetTheme_PreservesAllProperties()
    {
        // Arrange
        var tenant = new Tenant();
        var theme = new TenantTheme
        {
            PrimaryColor = "#123456",
            SecondaryColor = "#654321",
            DisplayName = "Test Org",
            FooterText = "Test Footer",
            HidePoweredBy = true,
            CustomCss = ".custom { background: blue; }"
        };

        // Act
        tenant.SetTheme(theme);
        var retrievedTheme = tenant.GetTheme();

        // Assert
        Assert.Equal("#123456", retrievedTheme.PrimaryColor);
        Assert.Equal("#654321", retrievedTheme.SecondaryColor);
        Assert.Equal("Test Org", retrievedTheme.DisplayName);
        Assert.Equal("Test Footer", retrievedTheme.FooterText);
        Assert.True(retrievedTheme.HidePoweredBy);
        Assert.Equal(".custom { background: blue; }", retrievedTheme.CustomCss);
    }

    [Fact]
    public void GetTheme_WithMaliciousCustomCss_PreservesRawValue()
    {
        // Arrange
        var tenant = new Tenant();
        var maliciousCss = "<script>alert('XSS')</script>.safe { color: red; }";
        var theme = new TenantTheme
        {
            CustomCss = maliciousCss
        };

        // Act
        tenant.SetTheme(theme);
        var retrievedTheme = tenant.GetTheme();

        // Assert - Raw value is preserved in storage
        Assert.Equal(maliciousCss, retrievedTheme.CustomCss);
        
        // But GetSanitizedCustomCss() returns safe version
        var sanitized = retrievedTheme.GetSanitizedCustomCss();
        Assert.DoesNotContain("<script>", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".safe", sanitized);
    }

    [Fact]
    public void GetTheme_WithInvalidJson_ReturnsDefaultTheme()
    {
        // Arrange
        var tenant = new Tenant
        {
            ThemeConfigJson = "{ invalid json"
        };

        // Act
        var theme = tenant.GetTheme();

        // Assert - Should return default theme without throwing
        Assert.NotNull(theme);
        Assert.Equal("#3B82F6", theme.PrimaryColor);
    }

    [Fact]
    public void SetTheme_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        var tenant = new Tenant();
        var originalUpdatedAt = tenant.UpdatedAt;
        
        // Wait a tiny bit to ensure timestamp changes
        System.Threading.Thread.Sleep(10);
        
        var theme = new TenantTheme
        {
            PrimaryColor = "#FF0000"
        };

        // Act
        tenant.SetTheme(theme);

        // Assert
        Assert.True(tenant.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void Tenant_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal(string.Empty, tenant.Name);
        Assert.Equal(string.Empty, tenant.Subdomain);
        Assert.Equal(SubscriptionTier.Free, tenant.SubscriptionTier);
        Assert.Equal(SubscriptionStatus.Active, tenant.SubscriptionStatus);
        Assert.True(tenant.IsActive);
        Assert.Equal("{}", tenant.ThemeConfigJson);
        Assert.Equal("{}", tenant.PaymentOptionsJson);
    }

    [Fact]
    public void GetTheme_PreservesCustomFields()
    {
        // Arrange
        var tenant = new Tenant();
        var theme = new TenantTheme();
        theme.CustomFields["key1"] = "value1";
        theme.CustomFields["key2"] = "value2";

        // Act
        tenant.SetTheme(theme);
        var retrievedTheme = tenant.GetTheme();

        // Assert
        Assert.Equal(2, retrievedTheme.CustomFields.Count);
        Assert.Equal("value1", retrievedTheme.CustomFields["key1"]);
        Assert.Equal("value2", retrievedTheme.CustomFields["key2"]);
    }

    [Fact]
    public void GetPaymentOptions_WithDefaultJson_ReturnsDefaultOptions()
    {
        var tenant = new Tenant
        {
            PaymentOptionsJson = "{}"
        };

        var paymentOptions = tenant.GetPaymentOptions();

        Assert.NotNull(paymentOptions);
        Assert.NotNull(paymentOptions.Stripe);
        Assert.False(paymentOptions.Stripe.Enabled);
        Assert.Null(paymentOptions.Stripe.AccountToken);
    }

    [Fact]
    public void SetPaymentOptions_PersistsStripeSettings_AndUpdatesTimestamp()
    {
        var tenant = new Tenant();
        var originalUpdatedAt = tenant.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        tenant.SetPaymentOptions(new TenantPaymentOptions
        {
            Stripe = new StripeTenantPaymentOptions
            {
                Enabled = true,
                AccountToken = "acct_test_1234"
            }
        });

        var paymentOptions = tenant.GetPaymentOptions();

        Assert.True(paymentOptions.Stripe.Enabled);
        Assert.Equal("acct_test_1234", paymentOptions.Stripe.AccountToken);
        Assert.True(tenant.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void GetPaymentOptions_WithInvalidJson_ReturnsDefaultOptions()
    {
        var tenant = new Tenant
        {
            PaymentOptionsJson = "{ invalid json"
        };

        var paymentOptions = tenant.GetPaymentOptions();

        Assert.NotNull(paymentOptions);
        Assert.NotNull(paymentOptions.Stripe);
        Assert.False(paymentOptions.Stripe.Enabled);
        Assert.Null(paymentOptions.Stripe.AccountToken);
    }
}
